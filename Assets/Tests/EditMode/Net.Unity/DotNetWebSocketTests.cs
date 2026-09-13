#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>Caminhos do adaptador real que não precisam de servidor.</summary>
    public class DotNetWebSocketTests
    {
        private static readonly Uri CleartextUrl = new Uri("ws://127.0.0.1:8000/ws/matchmaking/");

        private MainThreadQueue queue = null!;
        private List<string> events = null!;

        [SetUp]
        public void CreateQueue()
        {
            queue = new MainThreadQueue(new FakeClientLog());
            events = new List<string>();
        }

        [Test]
        public void CleartextRecusadoAvisaErroEFechaUmaVezSoDepoisDoDrain()
        {
            DotNetWebSocket socket = Socket(allowsCleartext: false);

            socket.Open(CleartextUrl);
            Assert.That(events, Is.Empty);
            queue.Drain();

            Assert.That(events, Is.EqualTo(new[] { "error:cleartext_refused", "closed:none:cleartext_refused" }));
        }

        [Test]
        public void FecharAntesDeAbrirAvisaUmFechamento()
        {
            DotNetWebSocket socket = Socket(allowsCleartext: false);

            socket.Close();
            socket.Close();
            queue.Drain();

            Assert.That(events, Is.EqualTo(new[] { "closed:none:closed_before_open" }));
        }

        [Test]
        public void EnviarAntesDeAbrirDevolveNotOpen()
        {
            DotNetWebSocket socket = Socket(allowsCleartext: false);

            Assert.That(socket.SendTextAsync("{}").Result, Is.SameAs(SocketSendOutcome.NotOpen));
        }

        [Test]
        public void AbrirDuasVezesLanca()
        {
            DotNetWebSocket socket = Socket(allowsCleartext: false);
            socket.Open(CleartextUrl);

            Assert.Throws<InvalidOperationException>(() => socket.Open(CleartextUrl));
        }

        private DotNetWebSocket Socket(bool allowsCleartext)
        {
            DotNetWebSocket socket = new DotNetWebSocket(queue, new CleartextPolicy(allowsCleartext), new FakeClientLog());
            socket.Opened += () => events.Add("opened");
            socket.Errored += detail => events.Add("error:" + detail);
            socket.Closed += closure => events.Add($"closed:{(closure.Code.HasValue ? closure.Code.Value.ToString() : "none")}:{closure.Reason}");
            return socket;
        }
    }
}
