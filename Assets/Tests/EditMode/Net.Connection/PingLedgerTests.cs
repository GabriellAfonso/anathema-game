#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class PingLedgerTests
    {
        private static readonly MonotonicInstant Start = new MonotonicInstant(TimeSpan.FromHours(1).Ticks);

        [Test]
        public void MarcadorLevaOInstanteEmMilissegundosESequenciaCrescente()
        {
            PingLedger ledger = new PingLedger();

            PingMarker first = ledger.Next(Start);
            PingMarker second = ledger.Next(Start.Add(TimeSpan.FromSeconds(10)));

            Assert.That(first.SentAtMs, Is.EqualTo(3_600_000));
            Assert.That((first.Sequence, second.Sequence), Is.EqualTo((1L, 2L)));
            Assert.That(second.SentAtMs, Is.EqualTo(3_610_000));
        }

        [Test]
        public void PongDoPendenteDevolveALatenciaERetiraOMarcador()
        {
            PingLedger ledger = new PingLedger();
            PingMarker marker = ledger.Next(Start);

            TimeSpan? latency = ledger.Match(marker, Start.Add(TimeSpan.FromMilliseconds(80)));

            Assert.That(latency, Is.EqualTo(TimeSpan.FromMilliseconds(80)));
            Assert.That(ledger.Match(marker, Start.Add(TimeSpan.FromSeconds(1))), Is.Null);
        }

        [Test]
        public void MarcadorDesconhecidoOuAusenteNaoMedeNada()
        {
            PingLedger ledger = new PingLedger();
            ledger.Next(Start);

            Assert.That(ledger.Match(null, Start), Is.Null);
            Assert.That(ledger.Match(new PingMarker(1, 99), Start), Is.Null);
        }

        [Test]
        public void MarcadorAlemDoLimiteDescartaOMaisVelho()
        {
            PingLedger ledger = new PingLedger();
            PingMarker oldest = ledger.Next(Start);
            PingMarker newest = oldest;
            for (int ping = 0; ping < PingLedger.MaxPending; ping++)
                newest = ledger.Next(Start);

            Assert.That(ledger.Match(oldest, Start), Is.Null);
            Assert.That(ledger.Match(newest, Start), Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void ClearEsqueceOsPendentes()
        {
            PingLedger ledger = new PingLedger();
            PingMarker marker = ledger.Next(Start);

            ledger.Clear();

            Assert.That(ledger.Match(marker, Start), Is.Null);
        }
    }
}
