#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>contracts/composition-and-scenes.md, "Composição": opções, salto de relógio, passo do quadro, slots e queda.</summary>
    public class ClientCompositionTests
    {
        private readonly List<ComposedClient> composed = new List<ComposedClient>();
        private FakeClientLog log = null!;

        [SetUp]
        public void CreateLog()
        {
            log = new FakeClientLog();
            composed.Clear();
        }

        [TearDown]
        public void DisposeComposed()
        {
            foreach (ComposedClient client in composed)
            {
                client.Client.Ports.Vault.Delete();
                client.Dispose();
            }
        }

        [Test]
        public void OpcoesSemRotasEComposicaoSemOpcoesLancam()
        {
            Assert.Throws<ArgumentNullException>(() => new ClientCompositionOptions(null!, true));
            Assert.Throws<ArgumentNullException>(() => ClientComposition.Compose(null!));
        }

        [Test]
        public void SaltoSemAOpcaoLancaCitandoAOpcao()
        {
            ComposedClient client = Compose("tests-composition-a", allowJumps: false);

            InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => client.JumpClock(TimeSpan.FromMinutes(5)));

            Assert.That(thrown.Message, Does.Contain("AllowClockJumps"));
        }

        [Test]
        public void SaltoComAOpcaoAvancaORelogioDaFachada()
        {
            ComposedClient client = Compose("tests-composition-a", allowJumps: true);
            MonotonicInstant before = client.Client.Ports.Clock.Now;

            client.JumpClock(TimeSpan.FromMinutes(5.5));

            Assert.That(client.Client.Ports.Clock.Now - before, Is.GreaterThanOrEqualTo(TimeSpan.FromMinutes(5.5)));
        }

        [Test]
        public void PassoDoQuadroDrenaAFilaAntesDoTique()
        {
            ComposedClient client = Compose("tests-composition-a", allowJumps: false);
            List<string> order = new List<string>();
            client.Client.Ports.Queue.Enqueue(() => order.Add("drain"));
            client.Client.Ports.Ticker.Ticked += () => order.Add("tick");

            client.Pump();

            Assert.That(order, Is.EqualTo(new[] { "drain", "tick" }));
        }

        [Test]
        public void SlotsDiferentesNaoDividemAGuarda()
        {
            ComposedClient first = Compose("tests-composition-a", allowJumps: false);
            ComposedClient second = Compose("tests-composition-b", allowJumps: false);

            first.Client.Ports.Vault.Save(new RefreshToken("refresh-a"));

            Assert.That(second.Client.Ports.Vault.Read().Kind, Is.EqualTo(VaultReadKind.Empty));
        }

        [Test]
        public void DerrubarSocketsAvisaQuedaSemCodigo()
        {
            ComposedClient client = Compose("tests-composition-a", allowJumps: false);
            List<SocketClosure> closures = new List<SocketClosure>();
            IWebSocket socket = client.Client.Ports.Sockets.Create();
            socket.Closed += closures.Add;

            client.DropSockets();

            Assert.That((closures.Count, closures[0].Code), Is.EqualTo((1, (int?)null)));
        }

        private ComposedClient Compose(string slot, bool allowJumps)
        {
            ClientCompositionOptions options = new ClientCompositionOptions(ServerRoutes.ForHost("http://127.0.0.1:8000", "ws://127.0.0.1:8000"), allowCleartext: true)
            {
                VaultSlot = RefreshTokenVaultSlot.Named(slot),
                Log = log,
                AllowClockJumps = allowJumps,
            };
            ComposedClient client = ClientComposition.Compose(options);
            composed.Add(client);
            return client;
        }
    }
}
#endif
