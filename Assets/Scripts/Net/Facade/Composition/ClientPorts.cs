#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// As portas de que a fachada precisa, juntas: I/O da 001, codec, guarda, rotas e margem de renovação. A borda
    /// real monta com os adaptadores Unity; os testes montam com os fakes nomeados
    /// (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// ClientPorts ports = new ClientPorts(http, sockets, clock, ticker, lifecycle, reachability, queue, log, codec, vault, accountRoutes, connectionRoutes, new AccountTiming());
    /// </code>
    /// </example>
    internal sealed class ClientPorts
    {
        /// <summary>Guarda as portas; nenhuma pode ser nula.</summary>
        /// <example><code>ClientPorts ports = new ClientPorts(http, sockets, clock, ticker, lifecycle, reachability, queue, log, codec, vault, accountRoutes, connectionRoutes, timing);</code></example>
        public ClientPorts(IHttpTransport http, IWebSocketFactory sockets, IMonotonicClock clock, IFrameTicker ticker, IAppLifecycle lifecycle,
            INetworkReachability reachability, MainThreadQueue queue, IClientLog log, IProtocolCodec codec, IRefreshTokenVault vault,
            AccountRoutes accountRoutes, ConnectionRoutes connectionRoutes, AccountTiming timing)
        {
            Http = Require(http, nameof(http));
            Sockets = Require(sockets, nameof(sockets));
            Clock = Require(clock, nameof(clock));
            Ticker = Require(ticker, nameof(ticker));
            Lifecycle = Require(lifecycle, nameof(lifecycle));
            Reachability = Require(reachability, nameof(reachability));
            Queue = Require(queue, nameof(queue));
            Log = Require(log, nameof(log));
            Codec = Require(codec, nameof(codec));
            Vault = Require(vault, nameof(vault));
            AccountRoutes = Require(accountRoutes, nameof(accountRoutes));
            ConnectionRoutes = Require(connectionRoutes, nameof(connectionRoutes));
            Timing = Require(timing, nameof(timing));
        }

        /// <summary>Transporte HTTP.</summary>
        /// <example><code>IHttpTransport http = ports.Http;</code></example>
        public IHttpTransport Http { get; }

        /// <summary>Fábrica de socket.</summary>
        /// <example><code>IWebSocketFactory sockets = ports.Sockets;</code></example>
        public IWebSocketFactory Sockets { get; }

        /// <summary>Relógio monotônico.</summary>
        /// <example><code>MonotonicInstant now = ports.Clock.Now;</code></example>
        public IMonotonicClock Clock { get; }

        /// <summary>Tique de quadro que move esperas.</summary>
        /// <example><code>ports.Ticker.Ticked += OnTick;</code></example>
        public IFrameTicker Ticker { get; }

        /// <summary>Ciclo de vida do app.</summary>
        /// <example><code>IAppLifecycle lifecycle = ports.Lifecycle;</code></example>
        public IAppLifecycle Lifecycle { get; }

        /// <summary>Alcançabilidade de rede.</summary>
        /// <example><code>NetworkKind kind = ports.Reachability.Current;</code></example>
        public INetworkReachability Reachability { get; }

        /// <summary>A troca única para a thread principal.</summary>
        /// <example><code>ports.Queue.Enqueue(() => Apply(result));</code></example>
        public MainThreadQueue Queue { get; }

        /// <summary>Log estruturado.</summary>
        /// <example><code>ports.Log.Info("client_composed");</code></example>
        public IClientLog Log { get; }

        /// <summary>Codec com os frames de fila e de partida.</summary>
        /// <example><code>string text = ports.Codec.Encode(message);</code></example>
        public IProtocolCodec Codec { get; }

        /// <summary>Guarda do refresh token.</summary>
        /// <example><code>VaultReadOutcome stored = ports.Vault.Read();</code></example>
        public IRefreshTokenVault Vault { get; }

        /// <summary>Rotas HTTP da conta.</summary>
        /// <example><code>Uri login = ports.AccountRoutes.Login;</code></example>
        public AccountRoutes AccountRoutes { get; }

        /// <summary>Rotas dos dois sockets.</summary>
        /// <example><code>Uri match = ports.ConnectionRoutes.Match;</code></example>
        public ConnectionRoutes ConnectionRoutes { get; }

        /// <summary>Margem de renovação do token de acesso.</summary>
        /// <example><code>TimeSpan margin = ports.Timing.RenewalMargin;</code></example>
        public AccountTiming Timing { get; }

        private static T Require<T>(T? value, string name) where T : class
        {
            return value ?? throw new ArgumentNullException(name, $"client port {name} is null: expected the {name} built by the composition");
        }
    }
}
