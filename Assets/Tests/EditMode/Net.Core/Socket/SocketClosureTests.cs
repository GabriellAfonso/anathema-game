#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class SocketClosureTests
    {
        [TestCase(4001)]
        [TestCase(4400)]
        [TestCase(4403)]
        [TestCase(4404)]
        [TestCase(1000)]
        public void CodigoDoServidorChegaExato(int code)
        {
            SocketClosure closure = new SocketClosure(code, "reason");

            Assert.That(closure.Code, Is.EqualTo(code));
            Assert.That(closure.HasServerCode, Is.True);
        }

        [Test]
        public void QuedaSemCloseFrameNaoTemCodigo()
        {
            SocketClosure closure = new SocketClosure(null, SocketClosure.AbnormalReason);

            Assert.That(closure.HasServerCode, Is.False);
            Assert.That(closure.ToString(), Is.EqualTo("close_code=none reason=abnormal"));
        }

        [Test]
        public void MotivoNuloLanca()
        {
            Assert.Throws<ArgumentNullException>(() => new SocketClosure(4001, null!));
        }

        [Test]
        public void EnvioComFalhaGuardaODetalhe()
        {
            SocketSendOutcome outcome = SocketSendOutcome.Failed("broken pipe");

            Assert.That(outcome.Status, Is.EqualTo(SocketSendStatus.Failed));
            Assert.That(outcome.FailureDetail, Is.EqualTo("broken pipe"));
            Assert.That(SocketSendOutcome.NotOpen.FailureDetail, Is.Null);
        }
    }
}
