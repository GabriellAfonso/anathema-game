#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A conta do jogador composta de uma vez: sessão, porta de token válido, cliente autenticado e
    /// os serviços de dados, mais a renovação na volta ao primeiro plano. Recebe as portas pelo
    /// construtor, para os testes comporem com fakes do mesmo jeito que o jogo compõe com os
    /// adaptadores reais. Veio de <c>Anathema.Net.Unity.LiveAccountServices</c>: a fachada, que é núcleo, compõe
    /// a conta (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// AccountServices account = new AccountServices(LivePorts.Create(adapters, lifecycle, reachability, ticker, vault, accountRoutes, connectionRoutes));
    /// ResumeOutcome resumed = await account.Session.ResumeAsync();
    /// </code>
    /// </example>
    internal sealed class AccountServices : IDisposable
    {
        private readonly ForegroundRenewal foregroundRenewal;

        /// <summary>Compõe a conta sobre as portas dadas.</summary>
        /// <example><code>AccountServices account = new AccountServices(ports);</code></example>
        public AccountServices(ClientPorts ports)
        {
            ClientPorts required = ports ?? throw new ArgumentNullException(nameof(ports), "ports are null: expected the ClientPorts built by the composition");
            Session = new AccountSession(required.Http, required.Codec, required.Clock, required.Vault, required.Log, required.AccountRoutes);
            SessionAccessTokens tokens = new SessionAccessTokens(Session, required.Clock, required.Timing);
            Tokens = tokens;
            Client = new AuthenticatedHttpClient(required.Http, Session, tokens);
            Profile = new OwnProfileQuery(Client, required.Codec, required.Log, required.AccountRoutes);
            Registration = new AccountRegistration(required.Http, required.Codec, required.Log, required.AccountRoutes);
            Catalog = new CardCatalog(Client, Session, required.Codec, required.Log, required.AccountRoutes);
            Decks = new PlayerDecks(Client, required.Codec, required.Log, required.AccountRoutes);
            History = new MatchHistory(Client, required.Codec, required.Log, required.AccountRoutes);
            foregroundRenewal = new ForegroundRenewal(required.Lifecycle, Session, tokens, required.Clock, required.Timing, required.Log);
        }

        /// <summary>Sessão de conta.</summary>
        /// <example><code>SignInOutcome outcome = await account.Session.SignInAsync(username, password);</code></example>
        public AccountSession Session { get; }

        /// <summary>Porta de token de acesso válido.</summary>
        /// <example><code>AccessTokenOutcome token = await account.Tokens.GetValidAsync();</code></example>
        public IAccessTokenSource Tokens { get; }

        /// <summary>Cliente HTTP autenticado.</summary>
        /// <example><code>AuthenticatedCallResult result = await account.Client.SendAsync(request);</code></example>
        public AuthenticatedHttpClient Client { get; }

        /// <summary>Leitura do próprio perfil.</summary>
        /// <example><code>AccountCallOutcome&lt;OwnProfile, ProfileRefusal&gt; profile = await account.Profile.ReadAsync();</code></example>
        public OwnProfileQuery Profile { get; }

        /// <summary>Cadastro de conta, sem sessão.</summary>
        /// <example><code>AccountCallOutcome&lt;AccountCreated, RegistrationRefusal&gt; registered = await account.Registration.RegisterAsync(form);</code></example>
        public AccountRegistration Registration { get; }

        /// <summary>Catálogo de cartas, carregado uma vez por sessão.</summary>
        /// <example><code>AccountCallOutcome&lt;LoadedCatalog, UnrecognizedRefusal&gt; cards = await account.Catalog.LoadAsync();</code></example>
        public CardCatalog Catalog { get; }

        /// <summary>Decks do jogador.</summary>
        /// <example><code>AccountCallOutcome&lt;IReadOnlyList&lt;PlayerDeck&gt;, DeckRefusal&gt; decks = await account.Decks.ListAsync();</code></example>
        public PlayerDecks Decks { get; }

        /// <summary>Histórico de partidas.</summary>
        /// <example><code>AccountCallOutcome&lt;MatchHistoryPage, HistoryRefusal&gt; page = await account.History.ReadPageAsync(new HistoryPageRequest());</code></example>
        public MatchHistory History { get; }

        /// <summary>Solta a assinatura do ciclo de vida.</summary>
        /// <example><code>account.Dispose();</code></example>
        public void Dispose() => foregroundRenewal.Dispose();
    }
}
