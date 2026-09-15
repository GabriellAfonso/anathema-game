#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A conta pela fachada: cadastrar, entrar, retomar sem senha, sair e ler o próprio perfil. Entrar e retomar
    /// levam o app a <see cref="ClientStage.SignedIn"/> pela thread principal; sair leva a
    /// <see cref="ClientStage.SignedOut"/> fechando fila e partida (specs/005-presentation-facade/contracts/client-state.md, "Conta").
    /// </summary>
    /// <example>
    /// <code>
    /// SignInOutcome outcome = await client.Account.SignInAsync(username, new Password(passwordText));
    /// if (outcome.Kind == SignInOutcomeKind.CredentialsRefused) ShowWrongPassword();
    /// </code>
    /// </example>
    public sealed class ClientAccount
    {
        private readonly AccountServices account;
        private readonly ClientStages stages;
        private readonly MainThreadQueue queue;
        private readonly Action<SignedOutReason, string> tearDown;

        internal ClientAccount(AccountServices account, ClientStages stages, MainThreadQueue queue, Action<SignedOutReason, string> tearDown)
        {
            this.account = account ?? throw new ArgumentNullException(nameof(account), "account is null: expected the composed AccountServices");
            this.stages = stages ?? throw new ArgumentNullException(nameof(stages), "stages are null: expected the client stages owner");
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");
            this.tearDown = tearDown ?? throw new ArgumentNullException(nameof(tearDown), "tear down is null: expected the client action that closes queue and match");
        }

        /// <summary>O estado da sessão de conta.</summary>
        /// <example><code>bool busy = client.Account.SessionState == AccountSessionState.SigningIn;</code></example>
        public AccountSessionState SessionState => account.Session.State;

        /// <summary>O próprio jogador; nulo sem sessão.</summary>
        /// <example><code>UserId? self = client.Account.Self;</code></example>
        public UserId? Self => account.Session.Self;

        /// <summary>Cadastra uma conta nova; não entra nem muda o estágio.</summary>
        /// <example><code>AccountCallOutcome&lt;AccountCreated, RegistrationRefusal&gt; created = await client.Account.RegisterAsync(form);</code></example>
        public Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>> RegisterAsync(RegistrationForm form) => account.Registration.RegisterAsync(form);

        /// <summary>Entra com usuário e senha; fora de <see cref="ClientStage.SignedOut"/> devolve <see cref="SignInOutcomeKind.AlreadySignedIn"/> sem enviar nada.</summary>
        /// <example><code>SignInOutcome outcome = await client.Account.SignInAsync("gabriel", new Password(text));</code></example>
        public async Task<SignInOutcome> SignInAsync(string username, Password password)
        {
            if (stages.State.Stage != ClientStage.SignedOut)
                return SignInOutcome.AlreadySignedIn(stages.State.Self!.Value);

            int generation = stages.Generation;
            SignInOutcome outcome = await account.Session.SignInAsync(username, password);
            if (outcome.Kind == SignInOutcomeKind.SignedIn)
                EnterSignedIn(outcome.User!.Value, generation, "signed_in");

            return outcome;
        }

        /// <summary>Retoma a sessão guardada sem pedir senha.</summary>
        /// <example><code>ResumeOutcome resumed = await client.Account.ResumeAsync();</code></example>
        public async Task<ResumeOutcome> ResumeAsync()
        {
            int generation = stages.Generation;
            ResumeOutcome outcome = await account.Session.ResumeAsync();
            if (outcome.Kind == ResumeOutcomeKind.Resumed)
                EnterSignedIn(outcome.User!.Value, generation, "resumed");

            return outcome;
        }

        /// <summary>Sai: fecha fila e partida de propósito, apaga a guarda e vai para o login; deslogado não faz nada.</summary>
        /// <example><code>StageRequestResult result = client.Account.SignOut();</code></example>
        public StageRequestResult SignOut()
        {
            if (stages.State.Stage == ClientStage.SignedOut)
                return StageRequestResult.NotApplicable(ClientStage.SignedOut);

            tearDown(SignedOutReason.SignedOut, "signed_out");
            account.Session.SignOut();
            return StageRequestResult.Done(ClientStage.SignedOut);
        }

        /// <summary>Lê o próprio perfil.</summary>
        /// <example><code>AccountCallOutcome&lt;OwnProfile, ProfileRefusal&gt; profile = await client.Account.ReadProfileAsync();</code></example>
        public Task<AccountCallOutcome<OwnProfile, ProfileRefusal>> ReadProfileAsync() => account.Profile.ReadAsync();

        private void EnterSignedIn(UserId user, int generation, string cause)
        {
            // A continuação pode não estar na thread principal: o estágio só muda na drenagem da fila (research R5).
            queue.Enqueue(() =>
            {
                if (generation == stages.Generation && stages.State.Stage == ClientStage.SignedOut)
                    stages.Move(ClientState.SignedIn(user), cause);
            });
        }
    }
}
