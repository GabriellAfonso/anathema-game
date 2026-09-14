#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionTimingTests
    {
        [Test]
        public void PadraoEhDezTrintaECinco()
        {
            ConnectionTiming timing = ConnectionTiming.Default;

            Assert.That(timing.PingInterval, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(timing.SilenceLimit, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(timing.PauseThreshold, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [TestCase(10, 10, 5, "silenceLimit")]
        [TestCase(10, 8, 5, "silenceLimit")]
        [TestCase(5, 30, 5, "pingInterval")]
        [TestCase(10, 30, 0, "pauseThreshold")]
        [TestCase(10, 30, -1, "pauseThreshold")]
        public void OrdemErradaLancaNomeandoOValor(int pingSeconds, int silenceSeconds, int pauseSeconds, string parameter)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ConnectionTiming(TimeSpan.FromSeconds(pingSeconds), TimeSpan.FromSeconds(silenceSeconds), TimeSpan.FromSeconds(pauseSeconds)));

            Assert.That(error.ParamName, Is.EqualTo(parameter));
            Assert.That(error.Message, Does.Contain("expected"));
        }
    }
}
