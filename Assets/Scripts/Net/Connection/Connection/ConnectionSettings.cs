#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Política de reconexão e tempos de uma conexão. Uma política por conexão: ela guarda a contagem
    /// de tentativas e de recusas daquele socket lógico.
    /// </summary>
    /// <example>
    /// <code>
    /// AuthenticatedConnection queue = new AuthenticatedConnection(ports, tokens, codec, ConnectionSettings.ForMatchmaking());
    /// </code>
    /// </example>
    internal sealed class ConnectionSettings
    {
        /// <summary>Configuração com a política e os tempos dados.</summary>
        /// <example><code>ConnectionSettings settings = new ConnectionSettings(new ReconnectPolicy(maxAttempts: 3), ConnectionTiming.Default);</code></example>
        public ConnectionSettings(ReconnectPolicy policy, ConnectionTiming timing)
        {
            Policy = policy ?? throw new ArgumentNullException(nameof(policy), "reconnect policy is null: expected one policy per connection");
            Timing = timing ?? throw new ArgumentNullException(nameof(timing), "connection timing is null: expected ConnectionTiming.Default or explicit values");
        }

        /// <summary>A política desta conexão.</summary>
        /// <example><code>int attempts = settings.Policy.MaxAttempts;</code></example>
        public ReconnectPolicy Policy { get; }

        /// <summary>Os tempos do heartbeat.</summary>
        /// <example><code>TimeSpan every = settings.Timing.PingInterval;</code></example>
        public ConnectionTiming Timing { get; }

        /// <summary>
        /// Fila: desiste rapido. Fila velha nao vale nada, e insistir em silencio e
        /// pior que devolver o jogador para a Home e deixar ele clicar de novo.
        /// </summary>
        /// <example><code>ConnectionSettings settings = ConnectionSettings.ForMatchmaking();</code></example>
        public static ConnectionSettings ForMatchmaking()
        {
            return new ConnectionSettings(
                new ReconnectPolicy(baseDelaySeconds: 0.5, maxDelaySeconds: 5.0, maxAttempts: 5),
                ConnectionTiming.Default);
        }

        /// <summary>
        /// Partida: a mais agressiva. Perder a partida por queda de rede e
        /// o pior resultado possivel, e o estado vive 6h no Redis esperando a volta.
        /// </summary>
        /// <example><code>ConnectionSettings settings = ConnectionSettings.ForMatch();</code></example>
        public static ConnectionSettings ForMatch()
        {
            return new ConnectionSettings(
                new ReconnectPolicy(baseDelaySeconds: 0.5, maxDelaySeconds: 15.0),
                ConnectionTiming.Default);
        }
    }
}
