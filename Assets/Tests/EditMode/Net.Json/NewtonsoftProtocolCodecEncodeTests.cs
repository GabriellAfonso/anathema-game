#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class NewtonsoftProtocolCodecEncodeTests
    {
        [Test]
        public void PingComMarcadorLevaOsDoisCampos()
        {
            string frame = Codec().Encode(new PingMessage(new PingMarker(1726000000000, 7)));

            IPayloadReader envelope = Reader(frame);
            Assert.That(envelope.ReadText("type"), Is.EqualTo("ping"));
            Assert.That(envelope.ReadObject("payload").ReadInteger("sent_at_ms"), Is.EqualTo(1726000000000));
            Assert.That(envelope.ReadObject("payload").ReadInteger("ping_seq"), Is.EqualTo(7));
        }

        [Test]
        public void PingSemMarcadorLevaPayloadVazio()
        {
            string frame = Codec().Encode(new PingMessage(null));

            Assert.That(Reader(frame).ReadObject("payload").FieldNames, Is.Empty);
        }

        [Test]
        public void EnvelopeSaiEmUmaLinha()
        {
            string frame = Codec().Encode(new PingMessage(new PingMarker(1, 2)));

            Assert.That(frame, Does.Not.Contain("\n").And.Not.Contain("\r"));
        }

        [Test]
        public void ObjetoSoltoParaCorpoHttp()
        {
            string body = Codec().EncodeObject(writer =>
            {
                writer.WriteText("username", "probe_1");
                writer.WriteText("password", "secret");
            });

            Assert.That(Reader(body).ReadText("username"), Is.EqualTo("probe_1"));
        }

        [Test]
        public void DecodeObjectRecusaTextoQueNaoEhObjeto()
        {
            Assert.That(Codec().DecodeObject("[1]").Failure.Kind, Is.EqualTo(DecodeFailureKind.NotObject));
            Assert.That(Codec().DecodeObject("<html>").Failure.Kind, Is.EqualTo(DecodeFailureKind.NotJson));
        }
    }
}
