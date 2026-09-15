#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US1-4, US1-5, US1-6, FR-012, FR-015: em partida no primeiro estado, queda sem aviso, fim com desfecho.</summary>
    public class MatchStageTests
    {
        private FacadeTestRig rig = null!;
        private List<ClientStageChange> changes = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            changes = new List<ClientStageChange>();
        }

        [Test]
        public async Task PrimeiroMatchStartLevaAEmPartidaComOEspelhoPreenchido()
        {
            await rig.ReachPairedAsync();
            rig.Client.StageChanged.Subscribe(changes.Add);

            rig.OpenLatest();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchStart);

            LiveMatch current = rig.Client.CurrentMatch!;
            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.InMatch));
            Assert.That((current.Mirror.Version, rig.Client.State.Match), Is.EqualTo(((long?)4, current)));
            Assert.That(rig.Sockets.Latest.OpenedUrl!.AbsolutePath, Is.EqualTo("/ws/match/"));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That((changes[0].Previous.Stage, changes[0].Current.Match), Is.EqualTo((ClientStage.Paired, current)));
        }

        [Test]
        public async Task QuedaEVoltaNaoMudamOEstagioNemAPartida()
        {
            await rig.ReachInMatchAsync();
            LiveMatch before = rig.Client.CurrentMatch!;
            int socketsBefore = rig.Sockets.Created.Count;
            rig.Client.StageChanged.Subscribe(changes.Add);

            rig.DropMatchSocketAndReopen();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchStart);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(socketsBefore + 1));
            Assert.That(before.Status.Phase, Is.EqualTo(LiveMatchPhase.Live));
            Assert.That((rig.Client.State.Stage, rig.Client.CurrentMatch, changes.Count), Is.EqualTo((ClientStage.InMatch, before, 0)));
        }

        [Test]
        public async Task FrameFinalLevaAoFimComDesfechoELinhaBuscando()
        {
            await rig.ReachInMatchAsync();
            LiveMatch current = rig.Client.CurrentMatch!;

            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);

            MatchResult result = rig.Client.State.Result!;
            Assert.That((rig.Client.State.Stage, rig.Client.CurrentMatch), Is.EqualTo((ClientStage.MatchFinished, current)));
            Assert.That((result.Won, result.Outcome!.DefeatedUser, result.RowStatus), Is.EqualTo((true, new UserId(9), HistoryRowStatus.Fetching)));
            Assert.That(result.Match.Value, Is.EqualTo(FacadeTestRig.MatchIdText));
        }

        [Test]
        public async Task PrimeiroFrameJaTerminadoVaiDePareadoAoFim()
        {
            await rig.ReachPairedAsync();
            rig.Client.StageChanged.Subscribe(changes.Add);

            rig.OpenLatest();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);

            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.MatchFinished));
            Assert.That((changes.Count, changes[0].Previous.Stage), Is.EqualTo((1, ClientStage.Paired)));
        }
    }
}
