#nullable enable
using System;
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// O único componente de cena da camada (FR-028): esvazia a fila da thread principal a
    /// cada quadro, consulta a rede, repassa pausa e foco, e fecha a fila ao ser destruído para
    /// nada ser entregue depois. Recebe as dependências por <see cref="Attach"/>; sem singleton.
    /// </summary>
    /// <example>
    /// <code>
    /// NetworkLayerHost host = new GameObject("NetworkLayer").AddComponent&lt;NetworkLayerHost&gt;();
    /// host.Attach(queue, lifecycle, reachability);
    /// </code>
    /// </example>
    public sealed class NetworkLayerHost : MonoBehaviour
    {
        private MainThreadQueue? queue;
        private UnityAppLifecycle? lifecycle;
        private UnityNetworkReachability? reachability;

        /// <summary>Liga o hospedeiro às peças da camada. Antes disso, ele não faz nada.</summary>
        /// <example><code>host.Attach(queue, UnityAppLifecycle.Create(clock, queue), UnityNetworkReachability.Create(clock, queue));</code></example>
        public void Attach(MainThreadQueue mainThreadQueue, UnityAppLifecycle appLifecycle, UnityNetworkReachability networkReachability)
        {
            queue = mainThreadQueue ?? throw new ArgumentNullException(nameof(mainThreadQueue), "queue is null: expected the main thread queue");
            lifecycle = appLifecycle ?? throw new ArgumentNullException(nameof(appLifecycle), "lifecycle is null: expected the app lifecycle adapter");
            reachability = networkReachability ?? throw new ArgumentNullException(nameof(networkReachability), "reachability is null: expected the reachability adapter");
        }

        private void Update()
        {
            reachability?.Poll();
            queue?.Drain();
        }

        private void OnApplicationPause(bool paused) => lifecycle?.OnPause(paused);

        private void OnApplicationFocus(bool focused) => lifecycle?.OnFocus(focused);

        private void OnDestroy() => queue?.Close();
    }
}
