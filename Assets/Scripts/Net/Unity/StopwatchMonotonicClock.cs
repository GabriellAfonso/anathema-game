#nullable enable
using System;
using System.Diagnostics;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Relógio sobre <c>Stopwatch</c>. No Windows o <c>QueryPerformanceCounter</c> conta o
    /// tempo de sono; no Android não, e lá vale <see cref="BootTimeMonotonicClock"/>
    /// (specs/001-server-connection/research.md, R1).
    /// </summary>
    /// <example>
    /// <code>
    /// IMonotonicClock clock = new StopwatchMonotonicClock();
    /// MonotonicInstant now = clock.Now;
    /// </code>
    /// </example>
    internal sealed class StopwatchMonotonicClock : IMonotonicClock
    {
        /// <summary>Instante atual em ticks de 100 ns.</summary>
        /// <example><code>MonotonicInstant now = clock.Now;</code></example>
        public MonotonicInstant Now => new MonotonicInstant(ToTicks(Stopwatch.GetTimestamp()));

        private static long ToTicks(long timestamp)
        {
            // Divide em partes inteira e resto para não perder precisão nem estourar com uptime longo.
            long seconds = timestamp / Stopwatch.Frequency;
            long remainder = timestamp % Stopwatch.Frequency;
            return seconds * TimeSpan.TicksPerSecond + remainder * TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        }
    }
}
