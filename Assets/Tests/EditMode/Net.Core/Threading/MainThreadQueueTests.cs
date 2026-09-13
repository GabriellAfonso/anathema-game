#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class MainThreadQueueTests
    {
        private FakeClientLog log = null!;
        private MainThreadQueue queue = null!;

        [SetUp]
        public void CreateQueue()
        {
            log = new FakeClientLog();
            queue = new MainThreadQueue(log);
        }

        [Test]
        public void NadaEhEntregueAntesDoDrain()
        {
            int delivered = 0;

            queue.Enqueue(() => delivered++);

            Assert.That(delivered, Is.Zero);
        }

        [Test]
        public void EntregaNaOrdemDeEnfileiramento()
        {
            List<int> delivered = new List<int>();
            for (int item = 0; item < 5; item++)
            {
                int captured = item;
                queue.Enqueue(() => delivered.Add(captured));
            }

            queue.Drain();

            Assert.That(delivered, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }

        [Test]
        public void QuatroThreadsDezMilItensSemPerdaESemTrocarOrdemDeCadaProdutor()
        {
            List<(int producer, int sequence)> delivered = new List<(int, int)>();
            Thread[] producers = Enumerable.Range(0, 4).Select(producer => new Thread(() => Produce(producer, delivered))).ToArray();

            foreach (Thread producer in producers) producer.Start();
            foreach (Thread producer in producers) producer.Join();
            queue.Drain();

            Assert.That(delivered.Count, Is.EqualTo(10000));
            for (int producer = 0; producer < 4; producer++)
                Assert.That(delivered.Where(item => item.producer == producer).Select(item => item.sequence), Is.Ordered.And.Unique);
        }

        [Test]
        public void CloseDescartaPendentes()
        {
            int delivered = 0;
            queue.Enqueue(() => delivered++);

            queue.Close();
            queue.Drain();

            Assert.That(delivered, Is.Zero);
        }

        [Test]
        public void EnfileirarDepoisDeFecharNaoLancaNemEntrega()
        {
            int delivered = 0;
            queue.Close();

            Assert.DoesNotThrow(() => queue.Enqueue(() => delivered++));
            queue.Drain();

            Assert.That(delivered, Is.Zero);
        }

        [Test]
        public void ItemQueLancaVaiParaOLogEOsSeguintesSaoEntregues()
        {
            int delivered = 0;
            queue.Enqueue(() => throw new InvalidOperationException("subscriber bug"));
            queue.Enqueue(() => delivered++);

            queue.Drain();

            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(log.Single("main_thread_item_failed").Fields[1].Value, Is.EqualTo("subscriber bug"));
        }

        [Test]
        public void ItemEnfileiradoDuranteODrainFicaParaOProximo()
        {
            int second = 0;
            queue.Enqueue(() => queue.Enqueue(() => second++));

            queue.Drain();
            Assert.That(second, Is.Zero);

            queue.Drain();
            Assert.That(second, Is.EqualTo(1));
        }

        [Test]
        public void FecharDuranteODrainParaAEntrega()
        {
            int delivered = 0;
            queue.Enqueue(queue.Close);
            queue.Enqueue(() => delivered++);

            queue.Drain();

            Assert.That(delivered, Is.Zero);
            Assert.That(queue.IsClosed, Is.True);
        }

        [Test]
        public void CloseEhIdempotente()
        {
            queue.Close();

            Assert.DoesNotThrow(queue.Close);
        }

        private void Produce(int producer, List<(int producer, int sequence)> delivered)
        {
            for (int sequence = 0; sequence < 2500; sequence++)
            {
                int captured = sequence;
                queue.Enqueue(() => delivered.Add((producer, captured)));
            }
        }
    }
}
