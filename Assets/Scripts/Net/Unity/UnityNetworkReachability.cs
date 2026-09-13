#nullable enable
using System;
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// <see cref="INetworkReachability"/> sobre <c>Application.internetReachability</c>, lida a
    /// cada 1 s pelo <see cref="NetworkLayerHost"/>. A API só pode ser lida na thread principal
    /// (specs/001-server-connection/research.md, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// UnityNetworkReachability reachability = UnityNetworkReachability.Create(clock, queue);
    /// reachability.Changed += change => log.Info("network_kind_changed");
    /// </code>
    /// </example>
    public sealed class UnityNetworkReachability : INetworkReachability
    {
        private readonly NetworkKindTracker tracker;

        /// <summary>Cria sobre um rastreador já inicializado.</summary>
        /// <example><code>UnityNetworkReachability reachability = new UnityNetworkReachability(tracker, queue);</code></example>
        public UnityNetworkReachability(NetworkKindTracker tracker, MainThreadQueue queue)
        {
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker), "tracker is null: expected the network kind tracker");
            if (queue == null)
                throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");

            tracker.Changed += change => queue.Enqueue(() => Changed?.Invoke(change));
        }

        /// <summary>Tipo de rede da última leitura.</summary>
        /// <example><code>NetworkKind now = reachability.Current;</code></example>
        public NetworkKind Current => tracker.Current;

        /// <summary>A rede mudou.</summary>
        public event Action<NetworkKindChanged>? Changed;

        /// <summary>Cria lendo o tipo de rede atual. Chamar na thread principal.</summary>
        /// <example><code>UnityNetworkReachability reachability = UnityNetworkReachability.Create(clock, queue);</code></example>
        public static UnityNetworkReachability Create(IMonotonicClock clock, MainThreadQueue queue)
        {
            NetworkKind initial = NetworkKindMapping.From(Application.internetReachability);
            return new UnityNetworkReachability(new NetworkKindTracker(clock, initial), queue);
        }

        /// <summary>Consulta a rede se já passou o intervalo. Chamar no <c>Update</c>.</summary>
        /// <example><code>reachability.Poll();</code></example>
        public void Poll()
        {
            if (tracker.ShouldPoll())
                tracker.Observe(NetworkKindMapping.From(Application.internetReachability));
        }
    }
}
