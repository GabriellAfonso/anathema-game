#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class PayloadIdentityExtensionsTests
    {
        [Test]
        public void UserIdEmTextoEhTipoErrado()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader("{\"user_id\": \"7\"}").ReadUserId("user_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.WrongFieldType));
        }

        [Test]
        public void UserIdZeroEhValorInvalido()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader("{\"user_id\": 0}").ReadUserId("user_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
            Assert.That(error.Failure.Path, Is.EqualTo("user_id"));
        }

        [Test]
        public void MatchIdVazioEhValorInvalido()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader("{\"match_id\": \"\"}").ReadMatchId("match_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
        }

        [Test]
        public void IdaEVoltaPreservaOsTres()
        {
            string json = Codec().EncodeObject(writer =>
            {
                writer.WriteUserId("user_id", new UserId(7));
                writer.WriteCardInstanceId("card_instance_id", new CardInstanceId(0));
                writer.WriteMatchId("match_id", new MatchId("0b7c2f4e"));
            });

            IPayloadReader reader = Reader(json);
            Assert.That(reader.ReadUserId("user_id"), Is.EqualTo(new UserId(7)));
            Assert.That(reader.ReadCardInstanceId("card_instance_id"), Is.EqualTo(new CardInstanceId(0)));
            Assert.That(reader.ReadMatchId("match_id"), Is.EqualTo(new MatchId("0b7c2f4e")));
        }

        [Test]
        public void OpcionaisAusentesSaoNulos()
        {
            IPayloadReader reader = Reader("{\"match_id\": null}");

            Assert.That(reader.ReadOptionalUserId("user_id"), Is.Null);
            Assert.That(reader.ReadOptionalCardInstanceId("card_instance_id"), Is.Null);
            Assert.That(reader.ReadOptionalMatchId("match_id"), Is.Null);
        }
    }
}
