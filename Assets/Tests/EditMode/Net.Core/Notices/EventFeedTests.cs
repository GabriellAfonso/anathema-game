#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    /// <summary>FR-020, research R3 da 005: assinatura descartável, isolamento de ouvinte e descarte durante o aviso.</summary>
    public class EventFeedTests
    {
        private FakeClientLog log = null!;
        private EventFeed<int> feed = null!;

        [SetUp]
        public void CreateFeed()
        {
            log = new FakeClientLog();
            feed = new EventFeed<int>("score_changed", log);
        }

        [Test]
        public void EntregaNaOrdemDeAssinatura()
        {
            List<string> received = new List<string>();
            feed.Subscribe(value => received.Add("a" + value));
            feed.Subscribe(value => received.Add("b" + value));

            feed.Publish(7);

            Assert.That(received, Is.EqualTo(new[] { "a7", "b7" }));
        }

        [Test]
        public void AssinaturaDescartadaNaoRecebe()
        {
            List<int> received = new List<int>();
            IDisposable subscription = feed.Subscribe(received.Add);

            subscription.Dispose();
            feed.Publish(3);

            Assert.That(received, Is.Empty);
        }

        [Test]
        public void DescartarDentroDoAvisoCalaOOuvinteSeguinteDoMesmoAviso()
        {
            List<string> received = new List<string>();
            IDisposable? second = null;
            feed.Subscribe(_ => { received.Add("first"); second!.Dispose(); });
            second = feed.Subscribe(_ => received.Add("second"));

            feed.Publish(1);
            feed.Publish(2);

            Assert.That(received, Is.EqualTo(new[] { "first", "first" }));
        }

        [Test]
        public void DescartarDuasVezesNaoFalha()
        {
            IDisposable subscription = feed.Subscribe(_ => { });

            subscription.Dispose();

            Assert.DoesNotThrow(subscription.Dispose);
        }

        [Test]
        public void AssinarDuranteOAvisoSoRecebeNoProximo()
        {
            List<int> late = new List<int>();
            feed.Subscribe(_ => feed.Subscribe(late.Add));

            feed.Publish(1);
            Assert.That(late, Is.Empty);

            feed.Publish(2);
            Assert.That(late, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void ExcecaoDeUmOuvinteVaiParaOLogEOsOutrosRecebem()
        {
            List<int> received = new List<int>();
            feed.Subscribe(_ => throw new InvalidOperationException("quebrado"));
            feed.Subscribe(received.Add);

            feed.Publish(5);

            Assert.That(received, Is.EqualTo(new[] { 5 }));
            ClientLogEntry failure = log.Single("feed_listener_failed");
            Assert.That(failure.Fields.First(field => field.Name == "feed").Value, Is.EqualTo("score_changed"));
            Assert.That(failure.Fields.First(field => field.Name == "exception").Value, Is.EqualTo(nameof(InvalidOperationException)));
        }

        [Test]
        public void OuvinteNuloLancaCitandoOFeed()
        {
            ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => feed.Subscribe(null!));

            Assert.That(thrown.Message, Does.Contain("score_changed"));
        }

        [Test]
        public void NomeVazioOuLogNuloLancam()
        {
            Assert.Throws<ArgumentException>(() => new EventFeed<int>(" ", log));
            Assert.Throws<ArgumentNullException>(() => new EventFeed<int>("score_changed", null!));
        }
    }
}
