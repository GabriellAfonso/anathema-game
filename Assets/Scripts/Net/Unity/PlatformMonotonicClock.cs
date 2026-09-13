#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Escolhe o relógio de cada alvo: <c>CLOCK_BOOTTIME</c> no Android, <c>Stopwatch</c> no
    /// Windows e no editor. Os dois ramos são alvos do jogo; não há outro.
    /// </summary>
    /// <example>
    /// <code>
    /// IMonotonicClock clock = PlatformMonotonicClock.Create(log);
    /// </code>
    /// </example>
    public static class PlatformMonotonicClock
    {
        /// <summary>O relógio monotônico que conta o sono do aparelho neste alvo.</summary>
        /// <example><code>IMonotonicClock clock = PlatformMonotonicClock.Create(log);</code></example>
        public static IMonotonicClock Create(IClientLog log)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new BootTimeMonotonicClock(log);
#else
            return new StopwatchMonotonicClock();
#endif
        }
    }
}
