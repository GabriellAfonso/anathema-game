#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Client.Scenes.Tests
{
    /// <summary>FR-020: as assinaturas de uma cena descartadas juntas, uma vez.</summary>
    public class SceneSubscriptionsTests
    {
        [Test]
        public void DescartaTodasUmaVezEEsvazia()
        {
            SceneSubscriptions subscriptions = new SceneSubscriptions();
            CountingSubscription first = new CountingSubscription();
            CountingSubscription second = new CountingSubscription();
            subscriptions.Add(first);
            subscriptions.Add(second);

            subscriptions.DisposeAll();
            subscriptions.DisposeAll();

            Assert.That((first.Disposals, second.Disposals), Is.EqualTo((1, 1)));
        }

        [Test]
        public void AssinaturaNulaLanca()
        {
            Assert.Throws<ArgumentNullException>(() => new SceneSubscriptions().Add(null!));
        }

        private sealed class CountingSubscription : IDisposable
        {
            internal int Disposals { get; private set; }

            public void Dispose() => Disposals++;
        }
    }
}
