#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// A sessão de conta do jogador: entra, sai e expira; guarda o par de tokens em memória e o
    /// refresh no aparelho. Roda só na thread principal, sem lock: as tarefas do transporte real
    /// completam nela (research R2). <see cref="Generation"/> muda a cada entrar, sair, expirar e
    /// retomar, para descartar resultado atrasado e invalidar cache de sessão.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountSession session = new AccountSession(http, codec, clock, vault, log, routes);
    /// session.SessionExpired += ShowLogin;
    /// SignInOutcome outcome = await session.SignInAsync("one", new Password("123456"));
    /// </code>
    /// </example>
    internal sealed class AccountSession
    {
        private readonly IHttpTransport http;
        private readonly IProtocolCodec codec;
        private readonly IMonotonicClock clock;
        private readonly IRefreshTokenVault vault;
        private readonly IClientLog log;
        private readonly AccountRoutes routes;
        private readonly AccessTokenReader tokenReader;
        private SessionTokens? tokens;
        private AccountSessionState settledState = AccountSessionState.SignedOut;
        private bool signingIn;

        /// <summary>Cria a sessão, deslogada, sobre as portas do núcleo.</summary>
        /// <example><code>AccountSession session = new AccountSession(http, codec, clock, vault, log, routes);</code></example>
        public AccountSession(IHttpTransport http, IProtocolCodec codec, IMonotonicClock clock, IRefreshTokenVault vault, IClientLog log, AccountRoutes routes)
        {
            this.http = Require(http, nameof(http));
            this.codec = Require(codec, nameof(codec));
            this.clock = Require(clock, nameof(clock));
            this.vault = Require(vault, nameof(vault));
            this.log = Require(log, nameof(log));
            this.routes = Require(routes, nameof(routes));
            tokenReader = new AccessTokenReader(codec);
            Refreshing = new RefreshCall(http, codec, clock, routes);
            Renewal = new TokenRenewal(this, Refreshing, log);
        }

        /// <summary>Emitido uma vez quando o servidor recusa o refresh token, na thread principal.</summary>
        public event Action? SessionExpired;

        /// <summary>Estado atual.</summary>
        /// <example><code>bool signedIn = session.State == AccountSessionState.SignedIn;</code></example>
        public AccountSessionState State => signingIn ? AccountSessionState.SigningIn : settledState;

        /// <summary>O jogador autenticado, do claim <c>user_id</c>; nulo sem sessão.</summary>
        /// <example><code>UserId? self = session.Self;</code></example>
        public UserId? Self { get; private set; }

        /// <summary>Muda a cada entrar, sair, expirar e retomar.</summary>
        /// <example><code>bool stale = loadedAt != session.Generation;</code></example>
        public int Generation { get; private set; }

        /// <summary>Texto do token de acesso atual, sem renovar; nulo sem sessão. Só para a ponte com o código antigo.</summary>
        /// <example><code>string? token = session.CurrentAccessTokenText;</code></example>
        public string? CurrentAccessTokenText => tokens?.Access.RevealForRequest();

        internal SessionTokens? CurrentTokens => tokens;

        internal TokenRenewal Renewal { get; }

        internal RefreshCall Refreshing { get; }

        internal SessionUnavailableKind UnavailableKind => settledState == AccountSessionState.Expired ? SessionUnavailableKind.Expired : SessionUnavailableKind.NoSession;

        /// <summary>Entra com usuário e senha; um login por vez (FR-008).</summary>
        /// <example><code>SignInOutcome outcome = await session.SignInAsync("one", new Password("123456"));</code></example>
        public async Task<SignInOutcome> SignInAsync(string username, Password password)
        {
            if (signingIn)
                return SignInOutcome.AlreadyInProgress();

            signingIn = true;
            try
            {
                return await SendSignInAsync(username, password, Generation).ConfigureAwait(false);
            }
            finally
            {
                signingIn = false;
            }
        }

        /// <summary>Sai: apaga tokens e guarda, sem chamar o servidor e sem aviso de expiração (FR-007).</summary>
        /// <example><code>session.SignOut();</code></example>
        public void SignOut()
        {
            Clear(AccountSessionState.SignedOut);
            vault.Delete();
            log.Info("account_signed_out");
        }

        /// <summary>
        /// Retoma sem senha com o refresh guardado (FR-018, FR-019). Recusa apaga a guarda e fica
        /// deslogada, sem aviso de expiração (não havia sessão); falta de rede mantém a guarda.
        /// </summary>
        /// <example><code>ResumeOutcome resumed = await session.ResumeAsync();</code></example>
        public async Task<ResumeOutcome> ResumeAsync()
        {
            if (tokens != null && Self.HasValue)
                return ResumeOutcome.Resumed(Self.Value);

            RefreshToken? stored = ReadStoredRefresh();
            if (stored == null)
                return ResumeOutcome.NothingStored();

            int generation = Generation;
            RenewalOutcome renewed = await Refreshing.SendAsync(stored).ConfigureAwait(false);
            return ApplyResume(renewed, stored, generation);
        }

        internal bool ApplyRenewedAccess(int generation, AccessToken access)
        {
            if (generation != Generation || tokens == null)
                return false;

            tokens = tokens.WithAccess(access);
            return true;
        }

        internal bool Expire(int generation)
        {
            if (generation != Generation || tokens == null)
                return false;

            Clear(AccountSessionState.Expired);
            vault.Delete();
            log.Warning("session_expired");
            SessionExpired?.Invoke();
            return true;
        }

        internal void Establish(SessionTokens issued)
        {
            tokens = issued;
            Self = issued.Access.Owner;
            settledState = AccountSessionState.SignedIn;
            Generation++;
        }

        private async Task<SignInOutcome> SendSignInAsync(string username, Password password, int generation)
        {
            string body = codec.EncodeObject(writer => AccountRequestBodies.WriteLogin(writer, username, password));
            HttpOutcome outcome = await http.SendAsync(AccountRequestBodies.JsonPost(routes.Login, body)).ConfigureAwait(false);
            return MapSignIn(outcome, generation);
        }

        private SignInOutcome MapSignIn(HttpOutcome outcome, int generation)
        {
            if (outcome.AsFailure is TransportFailure failure)
                return Refuse(SignInOutcome.TransportFailed(failure));

            HttpResponse response = outcome.AsResponse!;
            if (response.Status == 401)
                return Refuse(SignInOutcome.CredentialsRefused());

            if (response.Status != 200)
                return Refuse(SignInOutcome.ServerRefused(response.Status));

            DecodeOutcome<SessionTokens> issued = AccountRequestBodies.ReadLoginTokens(codec, tokenReader, response.Body, clock.Now);
            return issued.IsValid ? ApplySignIn(issued.Value, generation) : Refuse(SignInOutcome.OutOfContract(issued.Failure));
        }

        private SignInOutcome ApplySignIn(SessionTokens issued, int generation)
        {
            // Sair durante o login muda a geração; o login atrasado não pode ressuscitar a sessão.
            if (generation == Generation)
            {
                Establish(issued);
                SaveRefresh(issued.Refresh);
                log.Info("account_signed_in", new LogField("user_id", issued.Access.Owner.Value));
            }

            return SignInOutcome.SignedIn(issued.Access.Owner);
        }

        private void SaveRefresh(RefreshToken refresh)
        {
            VaultWriteOutcome saved = vault.Save(refresh);
            if (saved.Kind == VaultWriteKind.Failed)
                log.Warning("vault_save_failed", new LogField("detail", saved.Detail));
        }

        private RefreshToken? ReadStoredRefresh()
        {
            VaultReadOutcome read = vault.Read();
            if (read.Kind == VaultReadKind.Unreadable)
            {
                vault.Delete();
                log.Warning("vault_unreadable", new LogField("detail", read.Detail));
            }

            return read.Token;
        }

        private ResumeOutcome ApplyResume(RenewalOutcome renewed, RefreshToken stored, int generation)
        {
            if (renewed.Kind == RenewalOutcomeKind.Unavailable)
            {
                log.Warning("account_resume_unavailable", new LogField("renewal", renewed.ToString()));
                return ResumeOutcome.Unavailable(renewed);
            }

            // Um login durante a retomada muda a geração: nem aplicar nem apagar a guarda dele.
            if (generation != Generation)
                return renewed.Kind == RenewalOutcomeKind.Renewed ? ResumeOutcome.Resumed(renewed.Token!.Owner) : ResumeOutcome.Refused();

            return renewed.Kind == RenewalOutcomeKind.Renewed ? Resume(new SessionTokens(renewed.Token!, stored)) : RefuseResume();
        }

        private ResumeOutcome Resume(SessionTokens resumed)
        {
            Establish(resumed);
            log.Info("account_resumed", new LogField("user_id", resumed.Access.Owner.Value));
            return ResumeOutcome.Resumed(resumed.Access.Owner);
        }

        private ResumeOutcome RefuseResume()
        {
            vault.Delete();
            log.Warning("account_resume_refused");
            return ResumeOutcome.Refused();
        }

        private SignInOutcome Refuse(SignInOutcome outcome)
        {
            log.Warning("account_sign_in_refused", new LogField("outcome", outcome.ToString()));
            return outcome;
        }

        private void Clear(AccountSessionState state)
        {
            tokens = null;
            Self = null;
            settledState = state;
            Generation++;
        }

        private static T Require<T>(T dependency, string name) where T : class
        {
            return dependency ?? throw new ArgumentNullException(name, $"account session {name} is null: expected the dependency from the composition");
        }
    }
}
