#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class HttpOutcomeTests
    {
        [TestCase(401)]
        [TestCase(503)]
        public void StatusDeErroEhRespostaNaoFalha(int status)
        {
            HttpOutcome outcome = new HttpResponse(status, "body");

            Assert.That(outcome.AsResponse!.Status, Is.EqualTo(status));
            Assert.That(outcome.AsResponse.Body, Is.EqualTo("body"));
            Assert.That(outcome.AsFailure, Is.Null);
        }

        [Test]
        public void FalhaDeTransporteNaoEhResposta()
        {
            HttpOutcome outcome = new TransportFailure(TransportFailureKind.Timeout, "Request timeout");

            Assert.That(outcome.AsResponse, Is.Null);
            Assert.That(outcome.AsFailure!.Kind, Is.EqualTo(TransportFailureKind.Timeout));
            Assert.That(outcome.AsFailure.Detail, Is.EqualTo("Request timeout"));
        }

        [TestCase(99)]
        [TestCase(600)]
        public void StatusForaDaFaixaHttpLanca(int status)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HttpResponse(status, ""));
        }
    }
}
