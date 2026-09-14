#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// As duas conexões do jogo e a fila, compostas sobre as mesmas portas: a de fila desiste rápido, a de
    /// partida insiste (research R15). Quem compõe é a raiz das cenas; a apresentação só assina eventos.
    /// </summary>
    /// <example>
    /// <code>
    /// LiveConnectionServices connections = LiveConnectionServices.FromAdapters(adapters, lifecycle, reachability, ticker, account.Tokens, routes);
    /// connections.Queue.Join(deck);
    /// </code>
    /// </example>
    public sealed class LiveConnectionServices : IDisposable
    {
        /// <summary>Conexões e fila sobre as portas dadas.</summary>
        /// <example><code>LiveConnectionServices connections = new LiveConnectionServices(ports, tokens, codec, routes);</code></example>
        public LiveConnectionServices(ConnectionPorts ports, IAccessTokenSource tokens, IProtocolCodec codec, ConnectionRoutes routes)
        {
            ConnectionPorts required = ports ?? throw new ArgumentNullException(nameof(ports), "ports are null: expected the live connection ports");
            Routes = routes ?? throw new ArgumentNullException(nameof(routes), "routes are null: expected AppConfig.BuildConnectionRoutes()");
            MatchmakingConnection = new AuthenticatedConnection(required, tokens, codec, ConnectionSettings.ForMatchmaking());
            MatchConnection = new AuthenticatedConnection(required, tokens, codec, ConnectionSettings.ForMatch());
            Queue = new MatchQueue(MatchmakingConnection, ConnectionTarget.Matchmaking(routes.Matchmaking), required.Log);
        }

        /// <summary>As rotas dos dois sockets.</summary>
        /// <example><code>Uri matchBase = connections.Routes.Match;</code></example>
        public ConnectionRoutes Routes { get; }

        /// <summary>Conexão do socket de fila.</summary>
        /// <example><code>connections.MatchmakingConnection.StatusChanged += OnQueueStatus;</code></example>
        public AuthenticatedConnection MatchmakingConnection { get; }

        /// <summary>Conexão do socket de partida.</summary>
        /// <example><code>connections.MatchConnection.Connect(ConnectionTarget.Match(connections.Routes.Match, match));</code></example>
        public AuthenticatedConnection MatchConnection { get; }

        /// <summary>A fila sobre a conexão de fila.</summary>
        /// <example><code>connections.Queue.Join(deck);</code></example>
        public MatchQueue Queue { get; }

        /// <summary>Compõe sobre os adaptadores reais.</summary>
        /// <example><code>LiveConnectionServices connections = LiveConnectionServices.FromAdapters(adapters, lifecycle, reachability, ticker, account.Tokens, routes);</code></example>
        public static LiveConnectionServices FromAdapters(LiveNetworkAdapters adapters, IAppLifecycle lifecycle, INetworkReachability reachability,
            IFrameTicker ticker, IAccessTokenSource tokens, ConnectionRoutes routes)
        {
            ConnectionPorts ports = new ConnectionPorts(adapters.Sockets, adapters.Clock, ticker, lifecycle, reachability, adapters.Queue, adapters.Log);
            return new LiveConnectionServices(ports, tokens, adapters.Codec, routes);
        }

        /// <summary>Fecha as duas conexões e para de ouvir.</summary>
        /// <example><code>connections.Dispose();</code></example>
        public void Dispose()
        {
            Queue.Dispose();
            MatchmakingConnection.Dispose();
            MatchConnection.Dispose();
        }
    }
}
