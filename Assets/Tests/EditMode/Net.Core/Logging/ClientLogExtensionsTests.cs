#nullable enable
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class ClientLogExtensionsTests
    {
        [TestCase(ClientLogLevel.Debug)]
        [TestCase(ClientLogLevel.Info)]
        [TestCase(ClientLogLevel.Warning)]
        [TestCase(ClientLogLevel.Error)]
        public void CadaAtalhoGravaONivelCorrespondente(ClientLogLevel level)
        {
            FakeClientLog log = new FakeClientLog();

            WriteAt(log, level);

            Assert.That(log.Single("socket_closed").Level, Is.EqualTo(level));
        }

        [Test]
        public void CamposSaemNaOrdemEmQueForamPassados()
        {
            FakeClientLog log = new FakeClientLog();

            log.Info("socket_closed", new LogField("close_code", 4001), new LogField("reason", "auth"));

            ClientLogEntry entry = log.Single("socket_closed");
            Assert.That(entry.Fields[0].Name, Is.EqualTo("close_code"));
            Assert.That(entry.Fields[0].Value, Is.EqualTo("4001"));
            Assert.That(entry.Fields[1].Name, Is.EqualTo("reason"));
        }

        [Test]
        public void NomeDeEventoVazioLanca()
        {
            FakeClientLog log = new FakeClientLog();

            Assert.Throws<System.ArgumentException>(() => log.Info(" "));
        }

        private static void WriteAt(FakeClientLog log, ClientLogLevel level)
        {
            switch (level)
            {
                case ClientLogLevel.Debug: log.Debug("socket_closed"); break;
                case ClientLogLevel.Info: log.Info("socket_closed"); break;
                case ClientLogLevel.Warning: log.Warning("socket_closed"); break;
                case ClientLogLevel.Error: log.Error("socket_closed"); break;
            }
        }
    }
}
