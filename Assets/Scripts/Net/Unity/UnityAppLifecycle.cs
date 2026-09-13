#nullable enable
using System;
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// <see cref="IAppLifecycle"/> alimentado pelos callbacks de pausa e foco que o
    /// <see cref="NetworkLayerHost"/> repassa. A decisão fica no <see cref="LifecycleSignalFilter"/>;
    /// os avisos saem pela fila, para nunca chegarem antes de um fechamento de socket que
    /// aconteceu antes deles (specs/001-server-connection/research.md, R2 e R8).
    /// </summary>
    /// <example>
    /// <code>
    /// UnityAppLifecycle lifecycle = UnityAppLifecycle.Create(clock, queue);
    /// host.Attach(queue, lifecycle, reachability);
    /// </code>
    /// </example>
    public sealed class UnityAppLifecycle : IAppLifecycle
    {
        private readonly LifecycleSignalFilter filter;

        /// <summary>Cria sobre um filtro já configurado.</summary>
        /// <example><code>UnityAppLifecycle lifecycle = new UnityAppLifecycle(new LifecycleSignalFilter(clock, false), queue);</code></example>
        public UnityAppLifecycle(LifecycleSignalFilter filter, MainThreadQueue queue)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter), "filter is null: expected the lifecycle signal filter");
            if (queue == null)
                throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");

            filter.WentToBackground += signal => queue.Enqueue(() => WentToBackground?.Invoke(signal));
            filter.ReturnedToForeground += signal => queue.Enqueue(() => ReturnedToForeground?.Invoke(signal));
        }

        /// <summary>O app saiu do primeiro plano.</summary>
        public event Action<WentToBackground>? WentToBackground;

        /// <summary>O app voltou.</summary>
        public event Action<ReturnedToForeground>? ReturnedToForeground;

        /// <summary>
        /// Cria para a plataforma atual: no Windows sem Run In Background, perder o foco para o
        /// player e conta como segundo plano; no Android, não.
        /// </summary>
        /// <example><code>UnityAppLifecycle lifecycle = UnityAppLifecycle.Create(clock, queue);</code></example>
        public static UnityAppLifecycle Create(IMonotonicClock clock, MainThreadQueue queue)
        {
            bool focusLossStopsPlayer = Application.platform != RuntimePlatform.Android && !Application.runInBackground;
            return new UnityAppLifecycle(new LifecycleSignalFilter(clock, focusLossStopsPlayer), queue);
        }

        /// <summary>Repasse de <c>OnApplicationPause</c>.</summary>
        /// <example><code>private void OnApplicationPause(bool paused) => lifecycle.OnPause(paused);</code></example>
        public void OnPause(bool paused) => filter.OnPause(paused);

        /// <summary>Repasse de <c>OnApplicationFocus</c>.</summary>
        /// <example><code>private void OnApplicationFocus(bool focused) => lifecycle.OnFocus(focused);</code></example>
        public void OnFocus(bool focused) => filter.OnFocus(focused);
    }
}
