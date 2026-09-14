#nullable enable
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeHttpTransportTests
    {
        private static readonly Uri CardsUrl = new Uri("http://127.0.0.1:8000/game/cards/");

        [Test]
        public void RespostaRoteirizadaDe401EhResposta()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            http.RespondNext(401, "{}");

            HttpOutcome outcome = http.SendAsync(new HttpRequestSpec("GET", CardsUrl)).Result;

            Assert.That(outcome.AsResponse!.Status, Is.EqualTo(401));
        }

        [Test]
        public void FalhaRoteirizadaEhFalhaDeTransporte()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            http.FailNext(TransportFailureKind.Timeout, "Request timeout");

            HttpOutcome outcome = http.SendAsync(new HttpRequestSpec("GET", CardsUrl)).Result;

            Assert.That(outcome.AsFailure!.Kind, Is.EqualTo(TransportFailureKind.Timeout));
        }

        [Test]
        public void RoteirosSaoConsumidosEmOrdem()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            http.RespondNext(200, "first");
            http.FailNext(TransportFailureKind.CannotConnect, "down");

            HttpOutcome first = http.SendAsync(new HttpRequestSpec("GET", CardsUrl)).Result;
            HttpOutcome second = http.SendAsync(new HttpRequestSpec("GET", CardsUrl)).Result;

            Assert.That(first.AsResponse!.Body, Is.EqualTo("first"));
            Assert.That(second.AsFailure!.Kind, Is.EqualTo(TransportFailureKind.CannotConnect));
        }

        [Test]
        public void PedidoSemRoteiroLancaComMetodoEUrl()
        {
            FakeHttpTransport http = new FakeHttpTransport();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => http.SendAsync(new HttpRequestSpec("POST", CardsUrl)));

            Assert.That(error.Message, Does.Contain("POST").And.Contain(CardsUrl.ToString()));
        }

        [Test]
        public void PedidosSaoRegistradosComCabecalhosECorpo()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            http.RespondNext(201, "");
            Dictionary<string, string> headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

            http.SendAsync(new HttpRequestSpec("POST", CardsUrl, headers, "{\"a\": 1}")).Wait();

            HttpRequestSpec request = http.Requests[0];
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Headers["Content-Type"], Is.EqualTo("application/json"));
            Assert.That(request.Body, Is.EqualTo("{\"a\": 1}"));
        }

        [Test]
        public void RespostaSeguradaFicaPendenteAteSoltar()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            HeldHttpResponse held = http.HoldNext();

            Task<HttpOutcome> pending = http.SendAsync(new HttpRequestSpec("POST", CardsUrl));

            Assert.That(http.Requests.Count, Is.EqualTo(1));
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(held.IsPending, Is.True);
            held.Release(200, "ok");
            Assert.That(pending.IsCompleted, Is.True);
            Assert.That(pending.Result.AsResponse!.Body, Is.EqualTo("ok"));
        }

        [Test]
        public void RespostaSeguradaPodeSoltarComoFalha()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            HeldHttpResponse held = http.HoldNext();
            Task<HttpOutcome> pending = http.SendAsync(new HttpRequestSpec("GET", CardsUrl));

            held.ReleaseFailure(TransportFailureKind.Timeout, "Request timeout");

            Assert.That(pending.Result.AsFailure!.Kind, Is.EqualTo(TransportFailureKind.Timeout));
        }

        [Test]
        public void SoltarDuasVezesLanca()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            HeldHttpResponse held = http.HoldNext();
            held.Release(200, "");

            Assert.Throws<InvalidOperationException>(() => held.Release(200, ""));
        }

        [Test]
        public void SeguradaERoteirizadaSaoConsumidasNaOrdem()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            http.HoldNext();
            http.RespondNext(201, "");

            Task<HttpOutcome> first = http.SendAsync(new HttpRequestSpec("GET", CardsUrl));
            Task<HttpOutcome> second = http.SendAsync(new HttpRequestSpec("GET", CardsUrl));

            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.Result.AsResponse!.Status, Is.EqualTo(201));
        }
    }
}
