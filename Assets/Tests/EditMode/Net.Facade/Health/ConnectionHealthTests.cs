#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US6-1 a US6-5, FR-007: reconectando, voltou, desistiu e latência, só da conexão ativa.</summary>
    public class ConnectionHealthTests
    {
        private const string Pong = "{\"type\": \"pong\", \"payload\": {}}";
        private const string AuthDeniedFrame = "{\"type\": \"auth_denied\", \"payload\": {\"error\": \"authentication required: invalid token\"}}";

        private FacadeTestRig rig = null!;
        private List<ReconnectingNotice> reconnecting = null!;
        private List<RecoveredNotice> recovered = null!;
        private List<GaveUpNotice> gaveUp = null!;
        private List<TimeSpan> latencies = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            reconnecting = new List<ReconnectingNotice>();
            recovered = new List<RecoveredNotice>();
            gaveUp = new List<GaveUpNotice>();
            latencies = new List<TimeSpan>();
            rig.Client.Health.Reconnecting.Subscribe(reconnecting.Add);
            rig.Client.Health.Recovered.Subscribe(recovered.Add);
            rig.Client.Health.GaveUp.Subscribe(gaveUp.Add);
            rig.Client.Health.LatencyMeasured.Subscribe(latencies.Add);
        }

        [Test]
        public async Task QuedaDaPartidaAvisaReconectandoEDepoisVoltou()
        {
            await rig.ReachInMatchAsync();
            rig.Receive(Pong);

            rig.Sockets.Latest.SimulateClosed(null, "queda");
            rig.Pump();
            Assert.That((reconnecting.Count, reconnecting[0].Attempt, recovered.Count), Is.EqualTo((1, 1, 0)));
            Assert.That(reconnecting[0].Wait, Is.GreaterThan(TimeSpan.Zero));

            rig.Advance(TimeSpan.FromSeconds(20));
            rig.OpenLatest();
            rig.Receive(Pong);

            Assert.That(recovered.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task SemRedeProcurandoAvisaComATentativaDaQueda()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            rig.Receive(Pong);
            rig.Sockets.Latest.SimulateClosed(null, "queda");
            rig.Pump();

            rig.Reachability.SimulateKind(NetworkKind.None);
            rig.Pump();

            ReconnectingNotice last = reconnecting.Last();
            Assert.That((last.Attempt, last.Wait), Is.EqualTo((1, TimeSpan.Zero)));
        }

        [Test]
        public async Task DesistenciaAvisaDesistiuComOTextoDoMotivo()
        {
            await rig.ReachInMatchAsync();
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed-1")));
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed-2")));

            for (int refusal = 0; refusal < 3; refusal++)
                RefuseLatestToken(firstIsOpen: refusal == 0);

            GaveUpNotice notice = gaveUp.Single();
            Assert.That(notice.Reason, Is.EqualTo(GiveUpReason.TokenRefusedRepeatedly(3)));
            Assert.That(notice.PlayerText, Is.EqualTo(GiveUpReason.TokenRefusedRepeatedly(3).PlayerText()));
        }

        [Test]
        public async Task LatenciaEhDaConexaoAtiva()
        {
            await rig.ReachInMatchAsync();
            string ping = rig.Sockets.Latest.SentTexts.Last(text => text.Contains("\"type\":\"ping\""));

            rig.Clock.Advance(TimeSpan.FromMilliseconds(40));
            rig.Receive(PongFor(ping));

            Assert.That(latencies.Count, Is.EqualTo(1));
            Assert.That(rig.Client.Health.LastLatency, Is.EqualTo(latencies[0]));
        }

        [Test]
        public async Task ConexaoQueNaoEhAAtivaNaoAvisaNada()
        {
            await rig.SignInAsync();
            rig.Client.ConnectionParts.MatchConnection.Connect(ConnectionTarget.Match(rig.Ports.ConnectionRoutes.Match, new MatchId("stray")));
            rig.Pump();
            rig.OpenLatest();

            rig.Sockets.Latest.SimulateClosed(null, "queda");
            rig.Pump();

            Assert.That((reconnecting.Count, recovered.Count, gaveUp.Count, rig.Client.Health.LastLatency), Is.EqualTo((0, 0, 0, (TimeSpan?)null)));
            rig.Client.ConnectionParts.MatchConnection.Leave();
        }

        private void RefuseLatestToken(bool firstIsOpen)
        {
            if (!firstIsOpen)
                rig.OpenLatest();

            rig.Sockets.Latest.SimulateText(AuthDeniedFrame);
            rig.Sockets.Latest.SimulateClosed(4001, "auth_denied");
            rig.Pump();
        }

        private static string PongFor(string pingText)
        {
            const string PayloadKey = "\"payload\":";
            int start = pingText.IndexOf(PayloadKey, StringComparison.Ordinal) + PayloadKey.Length;
            return "{\"type\":\"pong\",\"payload\":" + pingText.Substring(start, pingText.Length - start - 1) + "}";
        }
    }
}
