#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// A conta do jogador composta de uma vez: sessão, porta de token válido, cliente autenticado e
    /// os serviços de dados, mais a renovação na volta ao primeiro plano. Recebe as portas pelo
    /// construtor, para os testes comporem com fakes do mesmo jeito que o jogo compõe com os
    /// adaptadores reais.
    /// </summary>
    /// <example>
    /// <code>
    /// LiveAccountServices account = LiveAccountServices.FromAdapters(adapters, lifecycle, config.BuildAccountRoutes(), PlatformRefreshTokenVault.Create(log));
    /// ResumeOutcome resumed = await account.Session.ResumeAsync();
    /// </code>
    /// </example>
    public sealed class LiveAccountServices : IDisposable
    {
        private readonly ForegroundRenewal foregroundRenewal;

        /// <summary>Compõe a conta sobre as portas dadas.</summary>
        /// <example><code>LiveAccountServices account = new LiveAccountServices(http, codec, clock, lifecycle, log, routes, vault, new AccountTiming());</code></example>
        public LiveAccountServices(IHttpTransport http, IProtocolCodec codec, IMonotonicClock clock, IAppLifecycle lifecycle,
            IClientLog log, AccountRoutes routes, IRefreshTokenVault vault, AccountTiming timing)
        {
            Session = new AccountSession(http, codec, clock, vault, log, routes);
            SessionAccessTokens tokens = new SessionAccessTokens(Session, clock, timing);
            Tokens = tokens;
            Client = new AuthenticatedHttpClient(http, Session, tokens);
            Profile = new OwnProfileQuery(Client, codec, log, routes);
            Registration = new AccountRegistration(http, codec, log, routes);
            Catalog = new CardCatalog(Client, Session, codec, log, routes);
            Decks = new PlayerDecks(Client, codec, log, routes);
            History = new MatchHistory(Client, codec, log, routes);
            foregroundRenewal = new ForegroundRenewal(lifecycle, Session, tokens, clock, timing, log);
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

        /// <summary>Compõe sobre os adaptadores reais, com a margem padrão.</summary>
        /// <example><code>LiveAccountServices account = LiveAccountServices.FromAdapters(adapters, lifecycle, routes, vault);</code></example>
        public static LiveAccountServices FromAdapters(LiveNetworkAdapters adapters, IAppLifecycle lifecycle, AccountRoutes routes, IRefreshTokenVault vault)
        {
            return new LiveAccountServices(adapters.Http, adapters.Codec, adapters.Clock, lifecycle, adapters.Log, routes, vault, new AccountTiming());
        }

        /// <summary>Solta a assinatura do ciclo de vida.</summary>
        /// <example><code>account.Dispose();</code></example>
        public void Dispose() => foregroundRenewal.Dispose();
    }
}
