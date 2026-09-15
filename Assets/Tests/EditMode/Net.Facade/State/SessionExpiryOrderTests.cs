#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>research R7: desistência por sessão antes ou depois do aviso da sessão dá o mesmo estado, uma vez.</summary>
    public class SessionExpiryOrderTests
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
        public async Task DesistenciaAntesDoAvisoDaSessaoDaOMesmoEstadoUmaVez()
        {
            await rig.ReachInMatchAsync();
            rig.Client.StageChanged.Subscribe(changes.Add);

            bool handled = rig.Client.Expiry.NoticeGiveUp(GiveUpReason.SessionExpired());
            await rig.ExpireSessionAsync();

            Assert.That(handled, Is.True);
            Assert.That((rig.Client.State.Stage, rig.Client.State.SignedOutReason), Is.EqualTo((ClientStage.SignedOut, (SignedOutReason?)SignedOutReason.SessionExpired)));
            Assert.That(changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void SemSessaoJaDeslogadoNaoMudaNada()
        {
            rig.Client.StageChanged.Subscribe(changes.Add);

            bool handled = rig.Client.Expiry.NoticeGiveUp(GiveUpReason.NoSession());

            Assert.That((handled, changes.Count, rig.Client.State.SignedOutReason), Is.EqualTo((true, 0, (SignedOutReason?)SignedOutReason.Startup)));
        }

        [Test]
        public async Task MotivoQueNaoEhDeSessaoNaoDesloga()
        {
            await rig.ReachInMatchAsync();

            bool handled = rig.Client.Expiry.NoticeGiveUp(GiveUpReason.AttemptsExhausted(5));

            Assert.That((handled, rig.Client.State.Stage), Is.EqualTo((false, ClientStage.InMatch)));
        }
    }
}
