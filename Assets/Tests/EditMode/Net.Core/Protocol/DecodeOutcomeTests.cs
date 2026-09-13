#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class DecodeOutcomeTests
    {
        private static readonly DecodeFailure MissingCode = new DecodeFailure(DecodeFailureKind.MissingField, "payload.code", "code is missing: expected text");

        [Test]
        public void ValidoEntregaOValor()
        {
            DecodeOutcome<string> outcome = DecodeOutcome<string>.Valid("pong");

            Assert.That(outcome.IsValid, Is.True);
            Assert.That(outcome.Value, Is.EqualTo("pong"));
        }

        [Test]
        public void LerFalhaDeResultadoValidoLanca()
        {
            Assert.Throws<InvalidOperationException>(() => _ = DecodeOutcome<string>.Valid("pong").Failure);
        }

        [Test]
        public void LerValorDeResultadoInvalidoLanca()
        {
            Assert.Throws<InvalidOperationException>(() => _ = DecodeOutcome<string>.Invalid(MissingCode).Value);
        }

        [Test]
        public void FalhaGuardaCategoriaCaminhoEDetalhe()
        {
            DecodeFailure failure = DecodeOutcome<string>.Invalid(MissingCode).Failure;

            Assert.That(failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(failure.Path, Is.EqualTo("payload.code"));
            Assert.That(failure.Detail, Is.EqualTo("code is missing: expected text"));
            Assert.That(failure.ToString(), Is.EqualTo("MissingField at payload.code: code is missing: expected text"));
        }
    }
}
