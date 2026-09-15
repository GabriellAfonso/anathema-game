#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>FR-008: operação fora do estágio devolve "não se aplica" com o estágio, sem exceção e sem requisição.</summary>
    public class StageMismatchTests
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
        public async Task SairDaFilaLogadoNaoSeAplica()
        {
            await rig.SignInAsync();
            rig.Client.StageChanged.Subscribe(changes.Add);

            AssertNotApplicable(rig.Client.Queue.Leave(), ClientStage.SignedIn);
        }

        [Test]
        public async Task VoltarProcurandoNaoSeAplica()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            rig.Client.StageChanged.Subscribe(changes.Add);

            AssertNotApplicable(rig.Client.ReturnToLobby(), ClientStage.Searching);
        }

        [Test]
        public void SairDaFilaEVoltarDeslogadoNaoSeAplicam()
        {
            rig.Client.StageChanged.Subscribe(changes.Add);

            AssertNotApplicable(rig.Client.Queue.Leave(), ClientStage.SignedOut);
            AssertNotApplicable(rig.Client.ReturnToLobby(), ClientStage.SignedOut);
            AssertNotApplicable(rig.Client.Account.SignOut(), ClientStage.SignedOut);
        }

        private void AssertNotApplicable(StageRequestResult result, ClientStage stage)
        {
            int requests = rig.Http.Requests.Count;
            Assert.That((result.Applied, result.Stage, rig.Client.State.Stage), Is.EqualTo((false, stage, stage)));
            Assert.That((changes.Count, rig.Http.Requests.Count), Is.EqualTo((0, requests)));
        }
    }
}
