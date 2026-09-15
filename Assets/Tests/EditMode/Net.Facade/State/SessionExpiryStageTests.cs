#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US2-4, FR-013, SC-002: refresh recusado em cada estágio logado leva ao login com motivo de expiração.</summary>
    public class SessionExpiryStageTests
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
        public async Task RefreshRecusadoLevaAoLoginFechandoTudo(string stage)
        {
            await rig.ReachStageAsync(stage);

            await rig.ExpireSessionAsync();

            Assert.That((rig.Client.State.Stage, rig.Client.State.SignedOutReason), Is.EqualTo((ClientStage.SignedOut, (SignedOutReason?)SignedOutReason.SessionExpired)));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
            Assert.That(rig.Client.Queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
            Assert.That(rig.Sockets.Created.All(socket => socket.CloseRequests > 0), Is.True, "todo socket aberto foi fechado de propósito");
        }

        [Test]
        public async Task CatalogoQueTerminaDepoisDaExpiracaoNaoAbreAPartida()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            HeldHttpResponse catalog = rig.Http.HoldNext();
            rig.Receive(FacadeTestRig.MatchFoundFrame);
            int created = rig.Sockets.Created.Count;

            await rig.ExpireSessionAsync();
            catalog.Release(200, FacadeCatalog.Body());
            rig.Pump();

            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.SignedOut));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(created));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
        }

        [Test]
        public async Task DepoisDeExpirarNenhumSocketReabre()
        {
            await rig.ReachInMatchAsync();
            await rig.ExpireSessionAsync();
            int created = rig.Sockets.Created.Count;

            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(created));
        }
    }
}
