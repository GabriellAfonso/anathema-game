#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class NewtonsoftProtocolCodecDecodeTests
    {
        [TestCase("", DecodeFailureKind.NotJson)]
        [TestCase("not json", DecodeFailureKind.NotJson)]
        [TestCase("{\"type\": \"pong\"", DecodeFailureKind.NotJson)]
        [TestCase("{} {}", DecodeFailureKind.NotJson)]
        [TestCase("[]", DecodeFailureKind.NotObject)]
        [TestCase("42", DecodeFailureKind.NotObject)]
        [TestCase("null", DecodeFailureKind.NotObject)]
        [TestCase("{}", DecodeFailureKind.MissingType)]
        [TestCase("{\"type\": 5}", DecodeFailureKind.TypeNotText)]
        [TestCase("{\"type\": null}", DecodeFailureKind.TypeNotText)]
        [TestCase("{\"type\": \"pong\", \"payload\": []}", DecodeFailureKind.PayloadNotObject)]
        [TestCase("{\"type\": \"pong\", \"payload\": \"x\"}", DecodeFailureKind.PayloadNotObject)]
        [TestCase("{\"type\": \"auth_denied\", \"payload\": {}}", DecodeFailureKind.MissingField)]
        [TestCase("{\"type\": \"auth_denied\", \"payload\": {\"error\": 1}}", DecodeFailureKind.WrongFieldType)]
        public void CadaPassoDoEnvelopeFalhaComSuaCategoria(string frame, DecodeFailureKind expected)
        {
            DecodeOutcome<ServerFrame> outcome = Codec().Decode(frame);

            Assert.That(outcome.IsValid, Is.False);
            Assert.That(outcome.Failure.Kind, Is.EqualTo(expected));
        }

        [Test]
        public void TextoNuloNaoLanca()
        {
            Assert.That(Codec().Decode(null!).Failure.Kind, Is.EqualTo(DecodeFailureKind.NotJson));
        }

        [Test]
        public void JsonMuitoProfundoNaoLanca()
        {
            string deep = new string('[', 1000) + new string(']', 1000);

            Assert.That(Codec().Decode(deep).IsValid, Is.False);
        }

        [Test]
        public void PayloadAusenteViraObjetoVazio()
        {
            DecodeOutcome<ServerFrame> outcome = Codec().Decode("{\"type\": \"pong\"}");

            Assert.That(outcome.Value, Is.InstanceOf<PongFrame>());
        }

        [Test]
        public void TypeComparadoComDiferencaDeMaiusculas()
        {
            DecodeOutcome<ServerFrame> outcome = Codec().Decode("{\"type\": \"Pong\", \"payload\": {}}");

            Assert.That(outcome.Value, Is.InstanceOf<UnknownServerFrame>());
            Assert.That(outcome.Value.MessageType, Is.EqualTo("Pong"));
        }

        [Test]
        public void CamposAMaisSaoIgnorados()
        {
            DecodeOutcome<ServerFrame> plain = Codec().Decode("{\"type\": \"auth_denied\", \"payload\": {\"error\": \"expired\"}}");
            DecodeOutcome<ServerFrame> extra = Codec().Decode("{\"type\": \"auth_denied\", \"version\": 3, \"payload\": {\"error\": \"expired\", \"hint\": {\"a\": [1]}}}");

            Assert.That(((AuthDeniedFrame)extra.Value).Error, Is.EqualTo(((AuthDeniedFrame)plain.Value).Error));
        }

        [Test]
        public void DetalheCortaOValorRecebido()
        {
            string huge = "x" + new string('y', 1000);

            DecodeFailure failure = Codec().Decode(huge).Failure;

            Assert.That(failure.Detail, Does.Not.Contain(new string('y', 201)));
        }

        [Test]
        public void ExcecaoInesperadaDeUmBracoViraFalhaELog()
        {
            FakeClientLog log = new FakeClientLog();
            DiscriminatedUnion<ServerFrame> frames = GenericServerFrames.CreateUnion()
                .Register("match_update", _ => throw new InvalidOperationException("bug in arm"));
            NewtonsoftProtocolCodec codec = new NewtonsoftProtocolCodec(frames, log);

            DecodeOutcome<ServerFrame> outcome = codec.Decode("{\"type\": \"match_update\", \"payload\": {}}");

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
            Assert.That(log.Single("codec_unexpected_failure").Level, Is.EqualTo(ClientLogLevel.Error));
        }
    }
}
