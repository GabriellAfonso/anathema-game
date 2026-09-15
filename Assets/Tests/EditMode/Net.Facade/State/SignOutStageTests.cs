#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US2-5, FR-010, FR-013: sair de cada estágio logado fecha tudo e vai para o login sem aviso de expiração.</summary>
    public class SignOutStageTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [TestCase("SignedIn")]
        [TestCase("Searching")]
        [TestCase("Paired")]
        [TestCase("InMatch")]
        [TestCase("MatchFinished")]
        [TestCase("MatchUnavailable")]
        public async Task SairFechaTudoEVaiParaOLogin(string stage)
        {
            await rig.ReachStageAsync(stage);

            StageRequestResult result = rig.Client.Account.SignOut();

            Assert.That((result.Applied, result.Stage), Is.EqualTo((true, ClientStage.SignedOut)));
            Assert.That(rig.Client.State.SignedOutReason, Is.EqualTo(SignedOutReason.SignedOut));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
            Assert.That((rig.Client.Queue.Phase, rig.Vault.Stored), Is.EqualTo((QueuePhase.OutOfQueue, (string?)null)));
            Assert.That(rig.Sockets.Created.All(socket => socket.CloseRequests > 0), Is.True, "todo socket aberto foi fechado de propósito");
            Assert.That(rig.Count("session_expired"), Is.Zero);
        }

        [Test]
        public async Task DepoisDeSairNenhumSocketReabre()
        {
            await rig.ReachInMatchAsync();
            rig.Client.Account.SignOut();
            int created = rig.Sockets.Created.Count;

            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(created));
        }

        [Test]
        public void SairDeslogadoNaoSeAplica()
        {
            StageRequestResult result = rig.Client.Account.SignOut();

            Assert.That((result.Applied, result.Stage), Is.EqualTo((false, ClientStage.SignedOut)));
            Assert.That(rig.Client.State.SignedOutReason, Is.EqualTo(SignedOutReason.Startup));
        }
    }
}
