#nullable enable
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>O log da prova: ordem, marca do salto e busca depois da marca (contracts/match-proof.md, passo 12).</summary>
    public class ProofLogTests
    {
        [Test]
        public void GuardaEmOrdemERepassaAoConsole()
        {
            FakeClientLog console = new FakeClientLog();
            ProofLog log = new ProofLog(console);

            log.Info("connection_opening");
            log.Info("connection_opened");

            Assert.That(log.Entries.Select(entry => entry.EventName), Is.EqualTo(new[] { "connection_opening", "connection_opened" }));
            Assert.That(console.Entries.Select(entry => entry.EventName), Is.EqualTo(new[] { "connection_opening", "connection_opened" }));
        }

        [Test]
        public void MarcaDevolveAPosicaoEVaiAoConsole()
        {
            FakeClientLog console = new FakeClientLog();
            ProofLog log = new ProofLog(console);
            log.Info("connection_opened");

            int mark = log.Mark("proof_clock_jump", new LogField("forward_ms", 330000L));

            Assert.That(mark, Is.EqualTo(1));
            Assert.That(log.Entries[mark].EventName, Is.EqualTo("proof_clock_jump"));
            Assert.That(console.Single("proof_clock_jump").Fields.Single().Value, Is.EqualTo("330000"));
        }

        [Test]
        public void BuscaSoDepoisDaMarca()
        {
            ProofLog log = new ProofLog(new FakeClientLog());
            log.Info("access_token_renewed");
            log.Info("connection_opening");
            int mark = log.Mark("proof_clock_jump");
            log.Info("socket_closed");
            log.Info("access_token_renewed");
            log.Info("connection_opening");

            Assert.That(log.IndexOfFirst("access_token_renewed", mark), Is.EqualTo(4));
            Assert.That(log.IndexOfFirst("connection_opening", mark), Is.EqualTo(5));
            Assert.That(log.IndexOfFirst("connection_gave_up", mark), Is.EqualTo(-1));
        }
    }
}
