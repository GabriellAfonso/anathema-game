#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-010, research R4: a tabela de vereditos.</summary>
    public class VersionGateTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void EspelhoVazioAceitaQualquerVersao(bool isStart)
        {
            Assert.That(new VersionGate().Judge(0, isStart), Is.EqualTo(FrameVerdict.Accept));
        }

        // O veredito é interno; o caso de teste público o recebe pelo nome.
        [TestCase(8, false, "Accept")]
        [TestCase(8, true, "Accept")]
        [TestCase(7, false, "Discard")]
        [TestCase(6, false, "Discard")]
        [TestCase(7, true, "ResyncSameVersion")]
        [TestCase(6, true, "Discard")]
        public void DepoisDaVersao7(long version, bool isStart, string expected)
        {
            VersionGate gate = new VersionGate();
            gate.Record(7);

            Assert.That(gate.Judge(version, isStart).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void JulgarNaoMudaAVersaoAplicada()
        {
            VersionGate gate = new VersionGate();
            gate.Record(7);

            gate.Judge(9, false);

            Assert.That(gate.AppliedVersion, Is.EqualTo(7));
        }

        [Test]
        public void RegistrarMudaAVersaoAplicada()
        {
            VersionGate gate = new VersionGate();

            gate.Record(12);

            Assert.That(gate.AppliedVersion, Is.EqualTo(12));
        }
    }
}
