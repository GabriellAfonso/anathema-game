#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// O <see cref="IFrameTicker"/> do jogo: o <see cref="NetworkLayerHost"/> chama <see cref="Raise"/> no
    /// fim de cada <c>Update</c>, depois de drenar a fila da thread principal.
    /// </summary>
    /// <example>
    /// <code>
    /// UnityFrameTicker ticker = new UnityFrameTicker();
    /// host.Attach(queue, lifecycle, reachability, ticker);
    /// </code>
    /// </example>
    public sealed class UnityFrameTicker : IFrameTicker
    {
        /// <inheritdoc />
        public event Action? Ticked;

        /// <summary>Passou um quadro.</summary>
        /// <example><code>ticker.Raise();</code></example>
        public void Raise()
        {
            Ticked?.Invoke();
        }
    }
}
