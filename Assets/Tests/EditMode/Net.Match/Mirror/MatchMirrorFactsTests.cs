#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US2-6, FR-013: fatos prontos para desenhar, com os nulos.</summary>
    public class MatchMirrorFactsTests
    {
        private MatchMirror mirror = null!;

        [SetUp]
        public void CreateMirror()
        {
            mirror = new MatchMirror(new FakeClientLog());
        }

        [Test]
        public void AntesDoPrimeiroFrameTudoFalsoOuNulo()
        {
            Assert.That(mirror.Phase, Is.Null);
            Assert.That((mirror.IsMyPriority, mirror.AmTokenHolder, mirror.MyMulliganPending, mirror.OpponentMulliganAnswered, mirror.IsFinished), Is.EqualTo((false, false, false, false, false)));
            Assert.That(mirror.DidIWin, Is.Null);
        }

        [Test]
        public void MulliganPendenteComOponenteJaRespondido()
        {
            mirror.Apply(MirrorViews.Mulligan(), 4, Array.Empty<MatchEvent>());

            Assert.That((mirror.MyMulliganPending, mirror.OpponentMulliganAnswered), Is.EqualTo((true, true)));
            Assert.That((mirror.IsMyPriority, mirror.AmTokenHolder), Is.EqualTo((false, false)));
        }

        [Test]
        public void PrioridadeETokenDoOponente()
        {
            mirror.Apply(MirrorViews.Action(), 5, Array.Empty<MatchEvent>());

            Assert.That((mirror.IsMyPriority, mirror.AmTokenHolder, mirror.MyMulliganPending), Is.EqualTo((false, false, false)));
        }

        [Test]
        public void PrioridadeETokenProprios()
        {
            mirror.Apply(MirrorViews.Declaration(), 9, Array.Empty<MatchEvent>());

            Assert.That((mirror.IsMyPriority, mirror.AmTokenHolder, mirror.IsFinished), Is.EqualTo((true, true, false)));
            Assert.That(mirror.DidIWin, Is.Null);
        }

        [Test]
        public void TerminadaComOponenteDerrotadoEhVitoria()
        {
            mirror.Apply(MirrorViews.Finished(), 20, Array.Empty<MatchEvent>());

            Assert.That((mirror.IsFinished, mirror.DidIWin), Is.EqualTo((true, (bool?)true)));
        }

        [Test]
        public void TerminadaComOProprioDerrotadoEhDerrota()
        {
            PlayerView lost = MirrorViews.Edited("contract-match-update-finished.json", "\"outcome\": {\"defeated_user_id\": 9", "\"outcome\": {\"defeated_user_id\": 7");

            mirror.Apply(lost, 20, Array.Empty<MatchEvent>());

            Assert.That(mirror.DidIWin, Is.False);
        }
    }
}
