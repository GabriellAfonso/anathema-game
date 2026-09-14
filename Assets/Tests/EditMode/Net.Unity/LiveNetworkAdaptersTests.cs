#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class LiveNetworkAdaptersTests
    {
        [Test]
        public void CompartilhaFilaLogEPolitica()
        {
            MainThreadQueue queue = new MainThreadQueue(new FakeClientLog());
            FakeClientLog log = new FakeClientLog();
            CleartextPolicy policy = new CleartextPolicy(false);

            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, policy);

            Assert.That(adapters.Queue, Is.SameAs(queue));
            Assert.That(adapters.Log, Is.SameAs(log));
            Assert.That(adapters.Policy, Is.SameAs(policy));
            Assert.That(adapters.Http, Is.InstanceOf<UnityHttpTransport>());
        }

        [Test]
        public void CadaSocketEhUmaInstanciaNova()
        {
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(new MainThreadQueue(new FakeClientLog()), new FakeClientLog(), new CleartextPolicy(false));

            Assert.That(adapters.CreateSocket(), Is.Not.SameAs(adapters.CreateSocket()));
        }

        [Test]
        public void SemPoliticaLanca()
        {
            Assert.Throws<ArgumentNullException>(() => LiveNetworkAdapters.Create(new MainThreadQueue(new FakeClientLog()), new FakeClientLog(), null!));
        }

        [Test]
        public void FabricaDeSocketCriaInstanciasNovas()
        {
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(new MainThreadQueue(new FakeClientLog()), new FakeClientLog(), new CleartextPolicy(false));

            Assert.That(adapters.Sockets.Create(), Is.Not.SameAs(adapters.Sockets.Create()));
        }

        [Test]
        public void CodecConheceOsFramesDaFila()
        {
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(new MainThreadQueue(new FakeClientLog()), new FakeClientLog(), new CleartextPolicy(false));

            DecodeOutcome<ServerFrame> decoded = adapters.Codec.Decode("{\"type\": \"matchmaking_failed\", \"payload\": {\"error\": \"x\"}}");

            Assert.That(decoded.Value, Is.InstanceOf<Anathema.Net.Connection.MatchmakingFailedFrame>());
        }

        [Test]
        public void CodecConheceOsFramesDaPartida()
        {
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(new MainThreadQueue(new FakeClientLog()), new FakeClientLog(), new CleartextPolicy(false));

            DecodeOutcome<ServerFrame> decoded = adapters.Codec.Decode("{\"type\": \"turn_warning\", \"payload\": {\"turn_number\": 12, \"holder_user_id\": 7, \"remaining_ms\": 15000}}");

            Assert.That(decoded.Value, Is.InstanceOf<Anathema.Net.Match.TurnWarningFrame>());
        }
    }
}
