#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um aviso que a apresentação assina e desfaz. <see cref="Subscribe"/> devolve algo descartável, para uma
    /// cena destruída não deixar assinatura pendurada; um ouvinte que lança vai para o log e não cala os outros
    /// (specs/005-presentation-facade/research.md, R3). Só a camada publica; a entrega acontece na thread
    /// principal porque quem publica já está nela.
    /// </summary>
    /// <example>
    /// <code>
    /// IDisposable subscription = client.StageChanged.Subscribe(change => Show(change.Current.Stage));
    /// subscription.Dispose();
    /// </code>
    /// </example>
    public sealed class EventFeed<T>
    {
        private readonly string name;
        private readonly IClientLog log;
        private readonly List<Listener> listeners = new List<Listener>();

        internal EventFeed(string name, IClientLog log)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"feed name is '{name}': expected a snake_case name like view_replaced", nameof(name));

            this.name = name;
            this.log = log ?? throw new ArgumentNullException(nameof(log), $"log of feed {name} is null: expected the client log that records failing listeners");
        }

        /// <summary>Passa a receber cada aviso publicado depois desta chamada, até descartar o retorno.</summary>
        /// <example><code>subscriptions.Add(match.Refused.Subscribe(refusal => ShowRefusal(refusal.Code)));</code></example>
        public IDisposable Subscribe(Action<T> listener)
        {
            Action<T> required = listener ?? throw new ArgumentNullException(nameof(listener), $"listener of feed {name} is null: expected an action that receives {typeof(T).Name}");
            Listener added = new Listener(this, required);
            listeners.Add(added);
            return added;
        }

        internal void Publish(T notice)
        {
            // Cópia: quem assina durante o aviso só recebe no próximo, e quem descarta sai já deste.
            foreach (Listener listener in listeners.ToArray())
            {
                if (!listener.IsDisposed)
                    Deliver(listener, notice);
            }
        }

        private void Deliver(Listener listener, T notice)
        {
            try
            {
                listener.Action(notice);
            }
            catch (Exception failure)
            {
                log.Error("feed_listener_failed", new LogField("feed", name), new LogField("exception", failure.GetType().Name), new LogField("message", failure.Message));
            }
        }

        private void Remove(Listener listener) => listeners.Remove(listener);

        private sealed class Listener : IDisposable
        {
            private readonly EventFeed<T> owner;

            internal Listener(EventFeed<T> owner, Action<T> action)
            {
                this.owner = owner;
                Action = action;
            }

            internal Action<T> Action { get; }

            internal bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                    return;

                IsDisposed = true;
                owner.Remove(this);
            }
        }
    }
}
