#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountCallOutcomeTests
    {
        [Test]
        public void SucessoTemSoOValor()
        {
            AccountCallOutcome<string, UnrecognizedRefusal> outcome = AccountCallOutcome<string, UnrecognizedRefusal>.Success("deck");

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Value, Is.EqualTo("deck"));
            Assert.That(outcome.Refusal, Is.Null);
            Assert.That(outcome.Failure, Is.Null);
        }

        [Test]
        public void RecusaTemSoARecusaELerOValorLancaComOEstado()
        {
            AccountCallOutcome<string, UnrecognizedRefusal> outcome = AccountCallOutcome<string, UnrecognizedRefusal>.Refused(new UnrecognizedRefusal(502, "<html>"));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => _ = outcome.Value);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Failure, Is.Null);
            Assert.That(error.Message, Does.Contain("refused").And.Contain("502"));
        }

        [Test]
        public void FalhaTemSoAFalha()
        {
            AccountCallFailure failure = AccountCallFailure.TransportFailed(new TransportFailure(TransportFailureKind.Timeout, "Request timeout"));

            AccountCallOutcome<string, UnrecognizedRefusal> outcome = AccountCallOutcome<string, UnrecognizedRefusal>.Failed(failure);

            Assert.That(outcome.Refusal, Is.Null);
            Assert.That(outcome.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.TransportFailed));
            Assert.That(outcome.Failure.Transport!.Kind, Is.EqualTo(TransportFailureKind.Timeout));
        }

        [Test]
        public void FalhaForaDoContratoGuardaStatusECaminho()
        {
            AccountCallFailure failure = AccountCallFailure.OutOfContract(200, new DecodeFailure(DecodeFailureKind.MissingField, "cards", "cards is missing"));

            Assert.That(failure.Status, Is.EqualTo(200));
            Assert.That(failure.ToString(), Does.Contain("MissingField").And.Contain("cards"));
        }

        [Test]
        public void RecusaNaoReconhecidaCortaOCorpoEmQuinhentos()
        {
            UnrecognizedRefusal refusal = new UnrecognizedRefusal(502, new string('x', 900));

            Assert.That(refusal.BodyText.Length, Is.EqualTo(UnrecognizedRefusal.MaxBodyLength));
        }
    }
}
