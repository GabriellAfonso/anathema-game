#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Os dois sockets do servidor, já com host e esquema do ambiente. Montado pela configuração do
    /// app; a camada nunca tem host fixo.
    /// </summary>
    /// <example>
    /// <code>
    /// ConnectionRoutes routes = config.BuildConnectionRoutes();
    /// ConnectionTarget queue = ConnectionTarget.Matchmaking(routes.Matchmaking);
    /// </code>
    /// </example>
    internal sealed class ConnectionRoutes
    {
        /// <summary>Rotas validadas: URLs absolutas <c>ws://</c> ou <c>wss://</c>.</summary>
        /// <example><code>ConnectionRoutes routes = new ConnectionRoutes(new Uri("ws://127.0.0.1:8000/ws/matchmaking/"), new Uri("ws://127.0.0.1:8000/ws/match/"));</code></example>
        public ConnectionRoutes(Uri matchmaking, Uri match)
        {
            Matchmaking = SocketUrl.RequireAbsoluteSocket(matchmaking, nameof(matchmaking));
            Match = SocketUrl.RequireAbsoluteSocket(match, nameof(match));
        }

        /// <summary>Socket da fila.</summary>
        /// <example><code>ConnectionTarget target = ConnectionTarget.Matchmaking(routes.Matchmaking);</code></example>
        public Uri Matchmaking { get; }

        /// <summary>Socket da partida, sem o <c>matchId</c>.</summary>
        /// <example><code>ConnectionTarget target = ConnectionTarget.Match(routes.Match, pairing.Match);</code></example>
        public Uri Match { get; }
    }
}
