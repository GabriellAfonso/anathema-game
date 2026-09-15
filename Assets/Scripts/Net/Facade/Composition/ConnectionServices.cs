#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// As duas conexões do jogo e a fila, compostas sobre as mesmas portas: a de fila desiste rápido, a de
    /// partida insiste (research R15 da 003). Veio de <c>Anathema.Net.Unity.LiveConnectionServices</c>: a fachada
    /// compõe as conexões e a apresentação só assina avisos (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// ConnectionServices connections = new ConnectionServices(ports, account.Tokens);
    /// connections.Queue.Join(deck);
    /// </code>
    /// </example>
    internal sealed class ConnectionServices : IDisposable
    {
        /// <summary>Conexões e fila sobre as portas dadas.</summary>
        /// <example><code>ConnectionServices connections = new ConnectionServices(ports, account.Tokens);</code></example>
        public ConnectionServices(ClientPorts ports, IAccessTokenSource tokens)
        {
            ClientPorts required = ports ?? throw new ArgumentNullException(nameof(ports), "ports are null: expected the ClientPorts built by the composition");
            IAccessTokenSource requiredTokens = tokens ?? throw new ArgumentNullException(nameof(tokens), "tokens are null: expected the account IAccessTokenSource");
            Routes = required.ConnectionRoutes;
            ConnectionPorts connectionPorts = new ConnectionPorts(required.Sockets, required.Clock, required.Ticker, required.Lifecycle, required.Reachability, required.Queue, required.Log);
            MatchmakingConnection = new AuthenticatedConnection(connectionPorts, requiredTokens, required.Codec, ConnectionSettings.ForMatchmaking());
            MatchConnection = new AuthenticatedConnection(connectionPorts, requiredTokens, required.Codec, ConnectionSettings.ForMatch());
            Queue = new MatchQueue(MatchmakingConnection, ConnectionTarget.Matchmaking(Routes.Matchmaking), required.Log);
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
