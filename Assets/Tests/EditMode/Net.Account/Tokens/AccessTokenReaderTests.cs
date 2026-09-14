#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccessTokenReaderTests
    {
        private static readonly MonotonicInstant ArrivedAt = new MonotonicInstant(42);

        [Test]
        public void TokenDoSimpleJwtDaDonoVidaEChegada()
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.Create(1000, 1300, "7"));

            Assert.That(token.Value.Owner, Is.EqualTo(new UserId(7)));
            Assert.That(token.Value.Lifetime, Is.EqualTo(TimeSpan.FromSeconds(300)));
            Assert.That(token.Value.ArrivedAt, Is.EqualTo(ArrivedAt));
        }

        [Test]
        public void UserIdComoInteiroJsonEhTipoErrado()
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.CreateRaw("{\"exp\":1300,\"iat\":1000,\"user_id\":7}"));

            Assert.That(token.Failure.Kind, Is.EqualTo(DecodeFailureKind.WrongFieldType));
            Assert.That(token.Failure.Path, Is.EqualTo("user_id"));
        }

        [TestCase("0")]
        [TestCase("-3")]
        [TestCase("abc")]
        public void UserIdQueNaoEhInteiroPositivoEhValorInvalido(string claim)
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.Create(1000, 1300, claim));

            Assert.That(token.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
        }

        [Test]
        public void SemExpEhCampoFaltando()
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.CreateRaw("{\"iat\":1000,\"user_id\":\"7\"}"));

            Assert.That(token.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(token.Failure.Path, Is.EqualTo("exp"));
        }

        [TestCase(1000, 1000)]
        [TestCase(1300, 1000)]
        public void ExpQueNaoVemDepoisDeIatEhValorInvalido(long issuedAt, long expiresAt)
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.Create(issuedAt, expiresAt));

            Assert.That(token.Failure.Kind, Is.EqualTo(DecodeFailureKind.InvalidValue));
        }

        [TestCase("header.payload")]
        [TestCase("a.b.c.d")]
        [TestCase("")]
        public void QuantidadeErradaDePartesNaoEhJson(string jwt)
        {
            Assert.That(Read(jwt).Failure.Kind, Is.EqualTo(DecodeFailureKind.NotJson));
        }

        [Test]
        public void PayloadForaDoBase64UrlNaoEhJson()
        {
            Assert.That(Read("header.@@@@.signature").Failure.Kind, Is.EqualTo(DecodeFailureKind.NotJson));
        }

        [Test]
        public void PayloadQueNaoEhObjetoFalhaSemEcoarOConteudo()
        {
            DecodeOutcome<AccessToken> token = Read(FakeAccessJwt.CreateRaw("[\"secret-claims\"]"));

            Assert.That(token.Failure.Kind, Is.EqualTo(DecodeFailureKind.NotObject));
            Assert.That(token.Failure.Detail, Does.Not.Contain("secret-claims"));
        }

        [Test]
        public void HoraDoServidorEmQualquerAnoDaAMesmaVida()
        {
            DecodeOutcome<AccessToken> year2000 = Read(FakeAccessJwt.Create(946_684_800, 946_685_100));
            DecodeOutcome<AccessToken> year2090 = Read(FakeAccessJwt.Create(3_786_912_000, 3_786_912_300));

            Assert.That(year2000.Value.Lifetime, Is.EqualTo(TimeSpan.FromSeconds(300)));
            Assert.That(year2090.Value.Lifetime, Is.EqualTo(year2000.Value.Lifetime));
        }

        private static DecodeOutcome<AccessToken> Read(string jwt)
        {
            return new AccessTokenReader(AccountTestCodec.Codec()).Read(jwt, ArrivedAt);
        }
    }
}
