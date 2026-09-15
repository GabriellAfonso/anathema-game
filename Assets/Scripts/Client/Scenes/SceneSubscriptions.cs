#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// As assinaturas de um componente de cena, para descartar juntas no <c>OnDestroy</c>: cena destruída não deixa
    /// assinatura pendurada na fachada (FR-020).
    /// </summary>
    /// <example>
    /// <code>
    /// private readonly SceneSubscriptions subscriptions = new SceneSubscriptions();
    /// public void BindClient(AnathemaClient client) => subscriptions.Add(client.StageChanged.Subscribe(Show));
    /// private void OnDestroy() => subscriptions.DisposeAll();
    /// </code>
    /// </example>
    public sealed class SceneSubscriptions
    {
        private readonly List<IDisposable> items = new List<IDisposable>();

        /// <summary>Guarda uma assinatura.</summary>
        /// <example><code>subscriptions.Add(client.Health.Recovered.Subscribe(_ => Hide()));</code></example>
        public void Add(IDisposable subscription)
        {
            items.Add(subscription ?? throw new ArgumentNullException(nameof(subscription), "subscription is null: expected the IDisposable returned by EventFeed.Subscribe"));
        }

        /// <summary>Descarta todas e esvazia; chamar de novo não faz nada.</summary>
        /// <example><code>private void OnDestroy() => subscriptions.DisposeAll();</code></example>
        public void DisposeAll()
        {
            foreach (IDisposable subscription in items)
                subscription.Dispose();

            items.Clear();
        }
    }
}
