#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US2-1, US2-2, FR-011, FR-012: substituição inteira e identidade própria.</summary>
    public class MatchMirrorApplyTests
    {
        private FakeClientLog log = null!;
        private MatchMirror mirror = null!;

        [SetUp]
        public void CreateMirror()
        {
            log = new FakeClientLog();
            mirror = new MatchMirror(log);
        }

        [Test]
        public void PrimeiroFrameDefineEstadoVersaoEIdentidade()
        {
            List<ViewReplaced> replaced = new List<ViewReplaced>();
            mirror.ViewReplaced += replaced.Add;
            PlayerView view = MirrorViews.Mulligan();

            mirror.Apply(view, 4, Array.Empty<MatchEvent>());

            Assert.That((mirror.Current, mirror.Version, mirror.Self), Is.EqualTo((view, (long?)4, (UserId?)new UserId(7))));
            Assert.That(replaced.Single().Previous, Is.Null);
            Assert.That(replaced.Single().Current, Is.SameAs(view));
        }

        [Test]
        public void VersaoComBuracoEhAplicadaSemEspera()
        {
            mirror.Apply(MirrorViews.Mulligan(), 4, Array.Empty<MatchEvent>());

            mirror.Apply(MirrorViews.Action(), 7, Array.Empty<MatchEvent>());

            Assert.That((mirror.Version, mirror.Phase), Is.EqualTo(((long?)7, (MatchPhase?)MatchPhase.Action)));
        }

        [Test]
        public void EstadoNovoSubstituiSemMesclar()
        {
            mirror.Apply(MirrorViews.Declaration(), 9, Array.Empty<MatchEvent>());

            mirror.Apply(MirrorViews.Action(), 10, Array.Empty<MatchEvent>());

            Assert.That(mirror.Current!.You.Bank.Select(unit => unit.Card.Instance), Is.EqualTo(new[] { new CardInstanceId(21) }));
            Assert.That(mirror.Current.Combat, Is.Null);
        }

        [Test]
        public void IdentidadeDiferenteEntreFramesRegistraEFicaANova()
        {
            mirror.Apply(MirrorViews.Action(), 5, Array.Empty<MatchEvent>());
            PlayerView other = MirrorViews.Edited("contract-match-update-declaration.json", "\"user_id\": 7, \"nickname\": \"gabriel\"", "\"user_id\": 8, \"nickname\": \"gabriel\"");

            mirror.Apply(other, 9, Array.Empty<MatchEvent>());

            Assert.That(mirror.Self, Is.EqualTo(new UserId(8)));
            Assert.That(log.Single("match_self_changed").Level, Is.EqualTo(ClientLogLevel.Error));
        }
    }
}
