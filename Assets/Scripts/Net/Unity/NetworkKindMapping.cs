#nullable enable
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>Tradução da alcançabilidade do Unity para o tipo do núcleo.</summary>
    /// <example>
    /// <code>
    /// NetworkKind kind = NetworkKindMapping.From(Application.internetReachability);
    /// </code>
    /// </example>
    public static class NetworkKindMapping
    {
        /// <summary>O <see cref="NetworkKind"/> equivalente.</summary>
        /// <example><code>NetworkKind kind = NetworkKindMapping.From(NetworkReachability.ReachableViaCarrierDataNetwork);</code></example>
        public static NetworkKind From(NetworkReachability reachability)
        {
            switch (reachability)
            {
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return NetworkKind.LocalArea;
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return NetworkKind.CarrierData;
                default:
                    return NetworkKind.None;
            }
        }
    }
}
