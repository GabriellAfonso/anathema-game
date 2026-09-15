#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Relógio que só avança e conta o tempo com o aparelho dormindo. Entra por
    /// interface para que o teste avance o tempo em vez de esperar
    /// (constituição, princípio VII). Seguro de qualquer thread.
    /// </summary>
    /// <example>
    /// <code>
    /// MonotonicInstant arrivedAt = clock.Now;
    /// </code>
    /// </example>
    internal interface IMonotonicClock
    {
        /// <summary>Instante atual. Leituras sucessivas nunca diminuem.</summary>
        /// <example><code>MonotonicInstant now = clock.Now;</code></example>
        MonotonicInstant Now { get; }
    }
}
