#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Valores de specs/003-authenticated-socket-queue/research.md, R15.</summary>
    public class ConnectionSettingsTests
    {
        [Test]
        public void FilaDesisteRapido()
        {
            ReconnectPolicy policy = ConnectionSettings.ForMatchmaking().Policy;

            Assert.That(policy.BaseDelaySeconds, Is.EqualTo(0.5));
            Assert.That(policy.MaxDelaySeconds, Is.EqualTo(5.0));
            Assert.That(policy.MaxAttempts, Is.EqualTo(5));
            Assert.That(policy.MaxAuthRetries, Is.EqualTo(2));
            Assert.That(policy.JitterRatio, Is.EqualTo(0.2));
        }

        [Test]
        public void PartidaInsisteSemLimite()
        {
            ReconnectPolicy policy = ConnectionSettings.ForMatch().Policy;

            Assert.That(policy.BaseDelaySeconds, Is.EqualTo(0.5));
            Assert.That(policy.MaxDelaySeconds, Is.EqualTo(15.0));
            Assert.That(policy.MaxAttempts, Is.EqualTo(int.MaxValue));
            Assert.That(policy.MaxAuthRetries, Is.EqualTo(2));
        }

        [Test]
        public void CadaChamadaTemPoliticaPropriaEOsTemposPadrao()
        {
            ConnectionSettings first = ConnectionSettings.ForMatch();
            ConnectionSettings second = ConnectionSettings.ForMatch();

            Assert.That(first.Policy, Is.Not.SameAs(second.Policy));
            Assert.That(first.Timing, Is.SameAs(ConnectionTiming.Default));
        }

        [Test]
        public void NulosLancam()
        {
            Assert.Throws<ArgumentNullException>(() => new ConnectionSettings(null!, ConnectionTiming.Default));
            Assert.Throws<ArgumentNullException>(() => new ConnectionSettings(new ReconnectPolicy(), null!));
        }
    }
}
