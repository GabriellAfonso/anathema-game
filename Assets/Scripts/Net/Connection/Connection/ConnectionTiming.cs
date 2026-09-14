#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Os três tempos do heartbeat da conexão. Intervalo e silêncio seguem o que o cliente já usava e
    /// o que a spec 013 do backend assume (ping a cada 10 s, queda depois de 30 s). O limiar de pausa
    /// separa um quadro lento de um app parado: acima dele, o tempo entre dois quadros não conta como
    /// silêncio do servidor (specs/003-authenticated-socket-queue/research.md, R3 e R8).
    /// </summary>
    /// <example>
    /// <code>
    /// ConnectionTiming timing = new ConnectionTiming(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    public sealed class ConnectionTiming
    {
        /// <summary>Tempos da camada: ping 10 s, silêncio 30 s, pausa 5 s.</summary>
        /// <example><code>ConnectionTiming timing = ConnectionTiming.Default;</code></example>
        public static ConnectionTiming Default { get; } = new ConnectionTiming(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));

        /// <summary>Tempos validados: pausa &gt; 0, intervalo &gt; pausa, silêncio &gt; intervalo.</summary>
        /// <example><code>ConnectionTiming fast = new ConnectionTiming(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(1));</code></example>
        public ConnectionTiming(TimeSpan pingInterval, TimeSpan silenceLimit, TimeSpan pauseThreshold)
        {
            RequirePositivePause(pauseThreshold);
            RequireLonger(nameof(pingInterval), pingInterval, nameof(pauseThreshold), pauseThreshold);
            RequireLonger(nameof(silenceLimit), silenceLimit, nameof(pingInterval), pingInterval);
            PingInterval = pingInterval;
            SilenceLimit = silenceLimit;
            PauseThreshold = pauseThreshold;
        }

        /// <summary>Espaço entre pings.</summary>
        /// <example><code>double seconds = timing.PingInterval.TotalSeconds;</code></example>
        public TimeSpan PingInterval { get; }

        /// <summary>Silêncio aceito, depois do primeiro pong, antes de derrubar.</summary>
        /// <example><code>double seconds = timing.SilenceLimit.TotalSeconds;</code></example>
        public TimeSpan SilenceLimit { get; }

        /// <summary>Intervalo entre quadros acima do qual o tempo é pausa, não silêncio.</summary>
        /// <example><code>bool paused = delta > timing.PauseThreshold;</code></example>
        public TimeSpan PauseThreshold { get; }

        private static void RequirePositivePause(TimeSpan pauseThreshold)
        {
            if (pauseThreshold <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(pauseThreshold), pauseThreshold, $"pause threshold is {pauseThreshold}: expected a positive duration");
        }

        private static void RequireLonger(string longerName, TimeSpan longer, string shorterName, TimeSpan shorter)
        {
            if (longer <= shorter)
                throw new ArgumentOutOfRangeException(longerName, longer, $"{longerName} is {longer} and {shorterName} is {shorter}: expected {longerName} longer than {shorterName}");
        }
    }
}
