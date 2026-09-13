#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O único ponto em que a camada troca de thread (constituição, princípio VI). Qualquer
    /// thread enfileira; a thread principal esvazia a cada quadro, na ordem de chegada.
    /// Depois de fechada, nada é entregue, para que um evento de socket nunca chegue a um
    /// objeto já destruído (specs/001-server-connection/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// // recepção do socket, fora da thread principal:
    /// queue.Enqueue(() => TextReceived?.Invoke(text));
    /// // NetworkLayerHost.Update, na thread principal:
    /// queue.Drain();
    /// </code>
    /// </example>
    public sealed class MainThreadQueue
    {
        private readonly ConcurrentQueue<Action> items = new ConcurrentQueue<Action>();
        private readonly IClientLog log;
        private int closed;

        /// <summary>Cria a fila aberta; falhas de item vão para o log.</summary>
        /// <example><code>MainThreadQueue queue = new MainThreadQueue(log);</code></example>
        public MainThreadQueue(IClientLog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log that records failing items");
        }

        /// <summary>Verdadeiro depois de <see cref="Close"/>.</summary>
        /// <example><code>if (queue.IsClosed) return;</code></example>
        public bool IsClosed => Volatile.Read(ref closed) == 1;

        /// <summary>Enfileira de qualquer thread. Fila fechada descarta sem exceção.</summary>
        /// <example><code>queue.Enqueue(() => Opened?.Invoke());</code></example>
        public void Enqueue(Action item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "queued item is null: expected an action to run on the main thread");

            if (!IsClosed)
                items.Enqueue(item);
        }

        /// <summary>
        /// Entrega, na thread principal, os itens que existiam no início da chamada. Item
        /// enfileirado durante a entrega fica para a próxima: sem reentrância.
        /// </summary>
        /// <example><code>private void Update() => queue.Drain();</code></example>
        public void Drain()
        {
            int pending = items.Count;
            for (int delivered = 0; delivered < pending && !IsClosed; delivered++)
            {
                if (!items.TryDequeue(out Action? item))
                    return;

                Run(item);
            }
        }

        /// <summary>Fecha e descarta os pendentes. Idempotente.</summary>
        /// <example><code>private void OnDestroy() => queue.Close();</code></example>
        public void Close()
        {
            if (Interlocked.Exchange(ref closed, 1) == 1)
                return;

            while (items.TryDequeue(out _))
            {
            }
        }

        private void Run(Action item)
        {
            try
            {
                item();
            }
            catch (Exception failure)
            {
                // Um assinante quebrado não pode calar os eventos dos outros (FR-027).
                log.Error("main_thread_item_failed", new LogField("exception", failure.GetType().Name), new LogField("message", failure.Message));
            }
        }
    }
}
