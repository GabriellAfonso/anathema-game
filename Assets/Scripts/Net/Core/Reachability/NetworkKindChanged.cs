#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// A rede mudou. Wi-Fi → dados móveis sem passar por "sem rede" é uma mudança
    /// só, e é a que derruba o socket em silêncio no Android.
    /// </summary>
    /// <example>
    /// <code>
    /// reachability.Changed += change =>
    /// {
    ///     if (change.Current != NetworkKind.None) ReconnectNow();
    /// };
    /// </code>
    /// </example>
    internal sealed class NetworkKindChanged
    {
        /// <summary>Cria a mudança; anterior igual ao novo lança.</summary>
        /// <example><code>NetworkKindChanged change = new NetworkKindChanged(NetworkKind.LocalArea, NetworkKind.CarrierData);</code></example>
        public NetworkKindChanged(NetworkKind previous, NetworkKind current)
        {
            if (previous == current)
                throw new ArgumentException($"network change is {previous} -> {current}: expected two different kinds", nameof(current));

            Previous = previous;
            Current = current;
        }

        /// <summary>Tipo de rede antes.</summary>
        /// <example><code>NetworkKind before = change.Previous;</code></example>
        public NetworkKind Previous { get; }

        /// <summary>Tipo de rede agora.</summary>
        /// <example><code>NetworkKind now = change.Current;</code></example>
        public NetworkKind Current { get; }
    }
}
