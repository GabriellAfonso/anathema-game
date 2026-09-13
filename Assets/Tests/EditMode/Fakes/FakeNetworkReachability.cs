#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>Alcançabilidade roteirizada; repetir o tipo atual não avisa, como no real.</summary>
    /// <example>
    /// <code>
    /// FakeNetworkReachability reachability = new FakeNetworkReachability(NetworkKind.LocalArea);
    /// reachability.SimulateKind(NetworkKind.CarrierData);
    /// </code>
    /// </example>
    public sealed class FakeNetworkReachability : INetworkReachability
    {
        /// <summary>Cria com o tipo de rede inicial.</summary>
        /// <example><code>FakeNetworkReachability offline = new FakeNetworkReachability(NetworkKind.None);</code></example>
        public FakeNetworkReachability(NetworkKind initial)
        {
            Current = initial;
        }

        /// <summary>Tipo de rede agora.</summary>
        /// <example><code>NetworkKind now = reachability.Current;</code></example>
        public NetworkKind Current { get; private set; }

        /// <summary>A rede mudou.</summary>
        public event Action<NetworkKindChanged>? Changed;

        /// <summary>Simula a rede passar a ser <paramref name="kind"/>.</summary>
        /// <example><code>reachability.SimulateKind(NetworkKind.None);</code></example>
        public void SimulateKind(NetworkKind kind)
        {
            if (kind == Current)
                return;

            NetworkKind previous = Current;
            Current = kind;
            Changed?.Invoke(new NetworkKindChanged(previous, kind));
        }
    }
}
