#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>Tipo de rede atual e aviso a cada mudança; nunca avisa sem mudança.</summary>
    /// <example>
    /// <code>
    /// reachability.Changed += change => log.Info("network_kind_changed",
    ///     new LogField("previous", change.Previous.ToString()), new LogField("current", change.Current.ToString()));
    /// </code>
    /// </example>
    internal interface INetworkReachability
    {
        /// <summary>Tipo de rede agora.</summary>
        /// <example><code>NetworkKind now = reachability.Current;</code></example>
        NetworkKind Current { get; }

        /// <summary>A rede mudou.</summary>
        event Action<NetworkKindChanged>? Changed;
    }
}
