#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class ClientLogLineFormatTests
    {
        [Test]
        public void EventoComCamposViraUmaLinhaChaveValor()
        {
            ClientLogEntry entry = Entry("socket_closed", new LogField("close_code", 4001), new LogField("server_code", true));

            Assert.That(ClientLogLineFormat.Format(entry), Is.EqualTo("socket_closed close_code=4001 server_code=true"));
        }

        [Test]
        public void ValorComEspacoSaiEntreAspas()
        {
            ClientLogEntry entry = Entry("socket_closed", new LogField("reason", "auth denied"));

            Assert.That(ClientLogLineFormat.Format(entry), Is.EqualTo("socket_closed reason=\"auth denied\""));
        }

        [Test]
        public void AspasDentroDoValorSaoEscapadas()
        {
            ClientLogEntry entry = Entry("codec_failed", new LogField("detail", "got \"x\""));

            Assert.That(ClientLogLineFormat.Format(entry), Is.EqualTo("codec_failed detail=\"got \\\"x\\\"\""));
        }

        [Test]
        public void ValorVazioSaiEntreAspas()
        {
            ClientLogEntry entry = Entry("socket_closed", new LogField("reason", ""));

            Assert.That(ClientLogLineFormat.Format(entry), Is.EqualTo("socket_closed reason=\"\""));
        }

        [Test]
        public void EventoSemCamposSaiSoComONome()
        {
            Assert.That(ClientLogLineFormat.Format(Entry("app_background")), Is.EqualTo("app_background"));
        }

        private static ClientLogEntry Entry(string eventName, params LogField[] fields)
        {
            return new ClientLogEntry(ClientLogLevel.Info, eventName, fields);
        }
    }
}
