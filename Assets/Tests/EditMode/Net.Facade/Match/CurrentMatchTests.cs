#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US5-4, FR-006: em partida, a partida corrente é a sessão ativa; fora dela, nenhuma.</summary>
    public class CurrentMatchTests
    {
        private const string NotYourPriority = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"not your priority\", \"code\": \"not_your_priority\"}}";

        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task EmPartidaAPartidaCorrenteTemEspelhoDicasERecusasAtivos()
        {
            await rig.ReachInMatchAsync();
            LiveMatch current = rig.Client.CurrentMatch!;
            List<PlayRefusal> refusals = new List<PlayRefusal>();
            current.Refused.Subscribe(refusals.Add);

            PlayerView view = current.Mirror.Current!;
            HandCardHint hint = current.HintFor(view.You.Hand[0].Instance);
            rig.Receive(NotYourPriority);

            Assert.That((current.Status.Phase, current.Mirror.Self), Is.EqualTo((LiveMatchPhase.Live, rig.Client.State.Self)));
            Assert.That(hint.Kind, Is.Not.EqualTo(HintCardKind.NotInHand));
            Assert.That((current.Pending.Current, current.Commands), Is.EqualTo(((PlayCommand?)null, current.Commands)));
            Assert.That(refusals.Count, Is.EqualTo(1));
            Assert.That(refusals[0].Code, Is.EqualTo(PlayRefusalCode.NotYourPriority));
        }

        [Test]
        public async Task LogadoOuProcurandoNaoTemPartidaCorrente()
        {
            await rig.SignInAsync();
            Assert.That(rig.Client.CurrentMatch, Is.Null);

            rig.JoinQueue();
            Assert.That(rig.Client.CurrentMatch, Is.Null);
        }

        [Test]
        public async Task PareadoComCatalogoCarregadoJaTemAPartidaCorrente()
        {
            await rig.ReachPairedAsync();

            Assert.That(rig.Client.CurrentMatch, Is.Not.Null);
            Assert.That(rig.Client.CurrentMatch!.Status.Phase, Is.EqualTo(LiveMatchPhase.Connecting));
        }
    }
}
