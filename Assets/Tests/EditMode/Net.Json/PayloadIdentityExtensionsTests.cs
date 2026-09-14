#nullable enable
using System.Collections.Generic;
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

        [Test]
        public void DeckIdECardIdSaoLidosDeInteiro()
        {
            IPayloadReader reader = Reader("{\"deck_id\": 4, \"card_id\": 1004}");

            Assert.That(reader.ReadDeckId("deck_id"), Is.EqualTo(new DeckId(4)));
            Assert.That(reader.ReadCardId("card_id"), Is.EqualTo(new CardId(1004)));
        }

        [TestCase("{\"deck_id\": \"4\"}", DecodeFailureKind.WrongFieldType)]
        [TestCase("{\"deck_id\": 0}", DecodeFailureKind.InvalidValue)]
        public void DeckIdComFormaErradaFalha(string json, DecodeFailureKind expected)
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader(json).ReadDeckId("deck_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(expected));
        }

        [TestCase("{\"card_id\": \"1004\"}", DecodeFailureKind.WrongFieldType)]
        [TestCase("{\"card_id\": 0}", DecodeFailureKind.InvalidValue)]
        public void CardIdComFormaErradaFalha(string json, DecodeFailureKind expected)
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader(json).ReadCardId("card_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(expected));
        }

        [Test]
        public void ListaDeCardIdPreservaOrdemERepeticao()
        {
            IReadOnlyList<CardId> cards = Reader("{\"card_ids\": [1, 1, 1004]}").ReadCardIdList("card_ids");

            Assert.That(cards, Is.EqualTo(new[] { new CardId(1), new CardId(1), new CardId(1004) }));
        }

        [Test]
        public void ListaDeCardIdComZeroApontaOItem()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Reader("{\"card_ids\": [1, 0]}").ReadCardIdList("card_ids"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
            Assert.That(error.Failure.Path, Is.EqualTo("card_ids[1]"));
        }

        [Test]
        public void IdaEVoltaDeDeckIdEListaDeCardId()
        {
            string json = Codec().EncodeObject(writer =>
            {
                writer.WriteDeckId("deck_id", new DeckId(4));
                writer.WriteCardIdList("card_ids", new[] { new CardId(9), new CardId(1001) });
            });

            IPayloadReader reader = Reader(json);
            Assert.That(reader.ReadDeckId("deck_id"), Is.EqualTo(new DeckId(4)));
            Assert.That(reader.ReadCardIdList("card_ids"), Is.EqualTo(new[] { new CardId(9), new CardId(1001) }));
        }
    }
}
