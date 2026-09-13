#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeClientLogTests
    {
        [Test]
        public void SingleDevolveAUnicaEntradaDoEvento()
        {
            FakeClientLog log = new FakeClientLog();
            log.Info("socket_opened");
            log.Warning("socket_closed", new LogField("close_code", 4001));

            Assert.That(log.Single("socket_closed").Fields[0].Value, Is.EqualTo("4001"));
        }

        [Test]
        public void SingleSemEntradaLanca()
        {
            FakeClientLog log = new FakeClientLog();

            Assert.Throws<InvalidOperationException>(() => log.Single("socket_closed"));
        }

        [Test]
        public void SingleComDuasEntradasLanca()
        {
            FakeClientLog log = new FakeClientLog();
            log.Info("socket_closed");
            log.Info("socket_closed");

            Assert.Throws<InvalidOperationException>(() => log.Single("socket_closed"));
        }

        [Test]
        public void EntriesEhUmaCopia()
        {
            FakeClientLog log = new FakeClientLog();
            int before = log.Entries.Count;

            log.Info("socket_opened");

            Assert.That(before, Is.Zero);
            Assert.That(log.Entries.Count, Is.EqualTo(1));
        }
    }
}
