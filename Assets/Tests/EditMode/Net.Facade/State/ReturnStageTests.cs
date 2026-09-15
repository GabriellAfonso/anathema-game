#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US1-7, FR-010: voltar do fim leva a logado sem partida; em partida não se aplica.</summary>
    public class ReturnStageTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task VoltarDoFimLevaALogadoSemPartida()
        {
            await rig.ReachInMatchAsync();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);

            StageRequestResult result = rig.Client.ReturnToLobby();

            Assert.That((result.Applied, result.Stage), Is.EqualTo((true, ClientStage.SignedIn)));
            Assert.That((rig.Client.State.Stage, rig.Client.State.Self), Is.EqualTo((ClientStage.SignedIn, (UserId?)new UserId(7))));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
        }

        [Test]
        public async Task VoltarEmPartidaNaoSeAplicaENadaMuda()
        {
            await rig.ReachInMatchAsync();

            StageRequestResult result = rig.Client.ReturnToLobby();

            Assert.That((result.Applied, result.Stage, rig.Client.State.Stage), Is.EqualTo((false, ClientStage.InMatch, ClientStage.InMatch)));
            Assert.That(rig.Client.CurrentMatch, Is.Not.Null);
        }

        [Test]
        public async Task DepoisDeVoltarNenhumSocketDePartidaReabre()
        {
            await rig.ReachInMatchAsync();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);
            rig.Client.ReturnToLobby();
            int created = rig.Sockets.Created.Count;

            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(created));
        }
    }
}
