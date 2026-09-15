#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>research R8 da 005: derrubar só os sockets vivos, como queda sem código, e repassar o resto.</summary>
    public class DroppableWebSocketFactoryTests
    {
        private static readonly Uri Url = new Uri("ws://127.0.0.1:8000/ws/match/");

        private FakeWebSocketFactory fakes = null!;
        private DroppableWebSocketFactory factory = null!;

        [SetUp]
        public void CreateFactory()
        {
            fakes = new FakeWebSocketFactory();
            factory = new DroppableWebSocketFactory(fakes);
        }

        [Test]
        public void DerrubarFechaOsVivosComQuedaSemCodigo()
        {
            List<SocketClosure> closures = new List<SocketClosure>();
            IWebSocket socket = OpenedSocket();
            socket.Closed += closures.Add;

            factory.DropAll();

            Assert.That(closures.Count, Is.EqualTo(1));
            Assert.That((closures[0].Code, closures[0].Reason), Is.EqualTo(((int?)null, DroppableWebSocket.DroppedReason)));
            Assert.That((fakes.Latest.CloseRequests, factory.LiveCount), Is.EqualTo((1, 0)));
        }

        [Test]
        public void FechadoPeloServidorSaiDaListaENaoEhDerrubadoDeNovo()
        {
            List<SocketClosure> closures = new List<SocketClosure>();
            IWebSocket socket = OpenedSocket();
            socket.Closed += closures.Add;

            fakes.Latest.SimulateClosed(1000, "bye");
            factory.DropAll();

            Assert.That(closures.Count, Is.EqualTo(1));
            Assert.That((closures[0].Code, fakes.Latest.CloseRequests, factory.LiveCount), Is.EqualTo(((int?)1000, 0, 0)));
        }

        [Test]
        public void RepassaAberturaETextoEDepoisDeDerrubadoNaoRepassaMais()
        {
            List<string> texts = new List<string>();
            IWebSocket socket = factory.Create();
            int opened = 0;
            socket.Opened += () => opened++;
            socket.TextReceived += texts.Add;
            socket.Open(Url);
            fakes.Latest.SimulateOpened();
            fakes.Latest.SimulateText("antes");

            factory.DropAll();
            fakes.Latest.SimulateText("depois");

            Assert.That((opened, fakes.Latest.OpenedUrl), Is.EqualTo((1, Url)));
            Assert.That(texts, Is.EqualTo(new[] { "antes" }));
        }

        [Test]
        public void DerrubarSemSocketsNaoFazNada()
        {
            Assert.DoesNotThrow(factory.DropAll);
            Assert.That(factory.LiveCount, Is.Zero);
        }

        private IWebSocket OpenedSocket()
        {
            IWebSocket socket = factory.Create();
            socket.Open(Url);
            fakes.Latest.SimulateOpened();
            return socket;
        }
    }
}
