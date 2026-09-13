#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    /// <summary>Frames genéricos dos contratos 009, 011 e 013 do backend.</summary>
    public class GenericServerFramesTests
    {
        [Test]
        public void RecusaPreservaCodigoErroECamposExtras()
        {
            ServerFrame frame = Codec().Decode("{\"type\": \"message_refused\", \"payload\": {\"error\": \"deck_id 4 is not a deck of user 9\", \"code\": \"deck_not_found\", \"deck_id\": 4}}").Value;

            MessageRefusedFrame refused = (MessageRefusedFrame)frame;
            Assert.That(refused.Code, Is.EqualTo("deck_not_found"));
            Assert.That(refused.Error, Is.EqualTo("deck_id 4 is not a deck of user 9"));
            Assert.That(refused.Details.ReadInteger("deck_id"), Is.EqualTo(4));
        }

        [Test]
        public void RecusaSemCodigoApontaPayloadCode()
        {
            DecodeFailure failure = Codec().Decode("{\"type\": \"message_refused\", \"payload\": {\"error\": \"x\"}}").Failure;

            Assert.That(failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(failure.Path, Is.EqualTo("payload.code"));
        }

        [Test]
        public void CodigoDeRecusaDesconhecidoEhPreservado()
        {
            ServerFrame frame = Codec().Decode("{\"type\": \"message_refused\", \"payload\": {\"error\": \"x\", \"code\": \"brand_new_code\"}}").Value;

            Assert.That(((MessageRefusedFrame)frame).Code, Is.EqualTo("brand_new_code"));
        }

        [Test]
        public void AuthDeniedLeOErro()
        {
            ServerFrame frame = Codec().Decode("{\"type\": \"auth_denied\", \"payload\": {\"error\": \"authentication required: invalid token\"}}").Value;

            Assert.That(((AuthDeniedFrame)frame).Error, Is.EqualTo("authentication required: invalid token"));
        }

        [Test]
        public void PongEcoaOMarcadorDoPing()
        {
            PingMarker sent = new PingMarker(1726000000000, 3);
            string echoed = Codec().EncodeObject(writer =>
            {
                writer.WriteText("type", "pong");
                writer.WriteObject("payload", new PingMessage(sent).WritePayload);
            });

            PongFrame pong = (PongFrame)Codec().Decode(echoed).Value;

            Assert.That(pong.Marker, Is.EqualTo(sent));
        }

        [TestCase("{}")]
        [TestCase("{\"sent_at_ms\": \"1\", \"ping_seq\": 2}")]
        [TestCase("{\"sent_at_ms\": 1}")]
        public void PongSemMarcadorInteiroTemMarcadorNulo(string payload)
        {
            PongFrame pong = (PongFrame)Codec().Decode("{\"type\": \"pong\", \"payload\": " + payload + "}").Value;

            Assert.That(pong.Marker, Is.Null);
        }

        [Test]
        public void MatchFoundAindaEhDesconhecido()
        {
            ServerFrame frame = Codec().Decode("{\"type\": \"match_found\", \"payload\": {\"match_id\": \"abc\"}}").Value;

            Assert.That(frame, Is.InstanceOf<UnknownServerFrame>());
        }
    }
}
