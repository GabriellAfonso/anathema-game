#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeWebSocketTests
    {
        private static readonly Uri MatchmakingUrl = new Uri("ws://127.0.0.1:8000/ws/matchmaking/?token=abc");

        [Test]
        public void OpenRegistraAUrl()
        {
            FakeWebSocket socket = new FakeWebSocket();

            socket.Open(MatchmakingUrl);

            Assert.That(socket.OpenedUrl, Is.EqualTo(MatchmakingUrl));
        }

        [Test]
        public void FechamentoCom4001ChegaUmaVezENadaDepois()
        {
            FakeWebSocket socket = OpenSocket();
            List<SocketClosure> closures = new List<SocketClosure>();
            socket.Closed += closures.Add;

            socket.SimulateClosed(4001, "auth_denied");
            socket.Close();

            Assert.That(closures.Count, Is.EqualTo(1));
            Assert.That(closures[0].Code, Is.EqualTo(4001));
            Assert.Throws<InvalidOperationException>(() => socket.SimulateText("late"));
        }

        [Test]
        public void EnviarAntesDeAbrirDevolveNotOpen()
        {
            FakeWebSocket socket = new FakeWebSocket();

            Assert.That(socket.SendTextAsync("{}").Result, Is.SameAs(SocketSendOutcome.NotOpen));
            Assert.That(socket.SentTexts, Is.Empty);
        }

        [Test]
        public void EnviosSaoGuardadosNaOrdem()
        {
            FakeWebSocket socket = OpenSocket();

            socket.SendTextAsync("first").Wait();
            socket.SendTextAsync("second").Wait();

            Assert.That(socket.SentTexts, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void EnvioRoteirizadoComFalhaNaoEhGuardado()
        {
            FakeWebSocket socket = OpenSocket();
            socket.NextSendOutcome = SocketSendOutcome.Failed("broken pipe");

            Assert.That(socket.SendTextAsync("lost").Result.Status, Is.EqualTo(SocketSendStatus.Failed));
            Assert.That(socket.SentTexts, Is.Empty);
        }

        [Test]
        public void FecharAntesDeAbrirAvisaUmaVezComMotivoLocal()
        {
            FakeWebSocket socket = new FakeWebSocket();
            List<SocketClosure> closures = new List<SocketClosure>();
            socket.Closed += closures.Add;

            socket.Close();
            socket.Close();

            Assert.That(closures.Count, Is.EqualTo(1));
            Assert.That(closures[0].Reason, Is.EqualTo(SocketClosure.ClosedBeforeOpenReason));
            Assert.That(socket.CloseRequests, Is.EqualTo(2));
        }

        [Test]
        public void AbrirDuasVezesLanca()
        {
            FakeWebSocket socket = OpenSocket();

            Assert.Throws<InvalidOperationException>(() => socket.Open(MatchmakingUrl));
        }

        [Test]
        public void TextoChegaParaOAssinante()
        {
            FakeWebSocket socket = OpenSocket();
            List<string> texts = new List<string>();
            socket.TextReceived += texts.Add;

            socket.SimulateText("{\"type\": \"pong\"}");

            Assert.That(texts, Is.EqualTo(new[] { "{\"type\": \"pong\"}" }));
        }

        private static FakeWebSocket OpenSocket()
        {
            FakeWebSocket socket = new FakeWebSocket();
            socket.Open(MatchmakingUrl);
            socket.SimulateOpened();
            return socket;
        }
    }
}
