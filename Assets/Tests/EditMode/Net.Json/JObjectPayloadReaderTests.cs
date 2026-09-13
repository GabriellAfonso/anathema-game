#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class JObjectPayloadReaderTests
    {
        [Test]
        public void CampoAusenteEhMissingFieldComCaminho()
        {
            IPayloadReader payload = Reader("{\"payload\": {}}").ReadObject("payload");

            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => payload.ReadText("code"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(error.Failure.Path, Is.EqualTo("payload.code"));
        }

        [TestCase("4.0")]
        [TestCase("\"4\"")]
        [TestCase("true")]
        public void InteiroSoAceitaInteiroJson(string raw)
        {
            IPayloadReader reader = Reader("{\"deck_id\": " + raw + "}");

            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => reader.ReadInteger("deck_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.WrongFieldType));
        }

        [Test]
        public void InteiroQueNaoCabeEm64BitsEhValorInvalido()
        {
            IPayloadReader reader = Reader("{\"deck_id\": 99999999999999999999}");

            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => reader.ReadInteger("deck_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
        }

        [Test]
        public void OpcionalAusenteOuNuloEhNulo()
        {
            IPayloadReader reader = Reader("{\"reason\": null}");

            Assert.That(reader.ReadOptionalText("reason"), Is.Null);
            Assert.That(reader.ReadOptionalInteger("deck_id"), Is.Null);
            Assert.That(reader.ReadOptionalObject("outcome"), Is.Null);
        }

        [Test]
        public void OpcionalComTipoErradoLanca()
        {
            IPayloadReader reader = Reader("{\"deck_id\": \"4\"}");

            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => reader.ReadOptionalInteger("deck_id"));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.WrongFieldType));
        }

        [Test]
        public void CaminhoAninhadoIncluiIndiceDaLista()
        {
            IPayloadReader payload = Reader("{\"payload\": {\"deck_problems\": [{\"kind\": \"a\"}, {\"kind\": 5}]}}").ReadObject("payload");

            IPayloadReader second = payload.ReadObjectList("deck_problems")[1];
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => second.ReadText("kind"));

            Assert.That(second.Path, Is.EqualTo("payload.deck_problems[1]"));
            Assert.That(error.Failure.Path, Is.EqualTo("payload.deck_problems[1].kind"));
        }

        [Test]
        public void ListaDeInteirosComItemErradoApontaOItem()
        {
            IPayloadReader reader = Reader("{\"card_ids\": [1, \"x\"]}");

            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => reader.ReadIntegerList("card_ids"));

            Assert.That(error.Failure.Path, Is.EqualTo("card_ids[1]"));
            Assert.That(Reader("{\"card_ids\": [1, 2, 3]}").ReadIntegerList("card_ids"), Is.EqualTo(new long[] { 1, 2, 3 }));
        }

        [Test]
        public void TextoComFormatoDeDataContinuaTexto()
        {
            IPayloadReader reader = Reader("{\"finished_at\": \"2026-09-13T10:00:00Z\"}");

            Assert.That(reader.ReadText("finished_at"), Is.EqualTo("2026-09-13T10:00:00Z"));
        }

        [Test]
        public void HasEFieldNamesListamOsCampos()
        {
            IPayloadReader reader = Reader("{\"code\": \"x\", \"error\": null, \"keep\": false}");

            Assert.That(reader.Has("error"), Is.True);
            Assert.That(reader.Has("deck_id"), Is.False);
            Assert.That(reader.FieldNames, Is.EquivalentTo(new[] { "code", "error", "keep" }));
            Assert.That(reader.ReadBoolean("keep"), Is.False);
        }
    }
}
