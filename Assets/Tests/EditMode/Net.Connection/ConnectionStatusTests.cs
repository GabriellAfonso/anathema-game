#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionStatusTests
    {
        [TestCase(ConnectionPhase.WaitingRetry)]
        [TestCase(ConnectionPhase.Suspended)]
        [TestCase(ConnectionPhase.GaveUp)]
        public void OfRecusaFaseQueTemDados(ConnectionPhase phase)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => ConnectionStatus.Of(phase));

            Assert.That(error.Message, Does.Contain(phase.ToString()));
        }

        [Test]
        public void WaitingValidaTentativaEEspera()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ConnectionStatus.Waiting(0, TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(-1)));
        }

        [Test]
        public void IgualdadePorValor()
        {
            Assert.That(ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1)), Is.EqualTo(ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1))));
            Assert.That(ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1)), Is.Not.EqualTo(ConnectionStatus.Waiting(3, TimeSpan.FromSeconds(1))));
            Assert.That(ConnectionStatus.GivenUp(GiveUpReason.SessionExpired()), Is.EqualTo(ConnectionStatus.GivenUp(GiveUpReason.SessionExpired())));
            Assert.That(ConnectionStatus.SuspendedBy(SuspensionReason.Background), Is.Not.EqualTo(ConnectionStatus.SuspendedBy(SuspensionReason.NoNetwork)));
            Assert.That(ConnectionStatus.Of(ConnectionPhase.Connected), Is.EqualTo(ConnectionStatus.Of(ConnectionPhase.Connected)));
        }

        [Test]
        public void DadosSoNaFaseQueOsTem()
        {
            ConnectionStatus connected = ConnectionStatus.Of(ConnectionPhase.Connected);
            ConnectionStatus suspended = ConnectionStatus.SuspendedBy(SuspensionReason.NoNetwork);

            Assert.That(connected.Attempt, Is.EqualTo(0));
            Assert.That(connected.GiveUp, Is.Null);
            Assert.That(suspended.Suspension, Is.EqualTo(SuspensionReason.NoNetwork));
            Assert.That(suspended.ToString(), Does.Contain("NoNetwork"));
        }
    }
}
