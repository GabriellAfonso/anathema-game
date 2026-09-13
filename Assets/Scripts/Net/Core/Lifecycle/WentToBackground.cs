#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>O app saiu do primeiro plano. No Android o socket vai cair.</summary>
    /// <example>
    /// <code>
    /// lifecycle.WentToBackground += signal => log.Info("app_background");
    /// </code>
    /// </example>
    public sealed class WentToBackground
    {
        /// <summary>Cria o aviso com o instante da saída.</summary>
        /// <example><code>WentToBackground signal = new WentToBackground(clock.Now);</code></example>
        public WentToBackground(MonotonicInstant at)
        {
            At = at;
        }

        /// <summary>Instante da saída, no relógio monotônico.</summary>
        /// <example><code>MonotonicInstant leftAt = signal.At;</code></example>
        public MonotonicInstant At { get; }
    }
}
