#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US2-5, US2-7, FR-014, FR-015: ordem dos avisos, fim uma vez e assinante que lança.</summary>
    public class MatchMirrorAnnouncementTests
    {
        private FakeClientLog log = null!;
        private MatchMirror mirror = null!;
        private List<string> notices = null!;

        [SetUp]
        public void CreateMirror()
        {
            log = new FakeClientLog();
            mirror = new MatchMirror(log);
            notices = new List<string>();
            mirror.ViewReplaced += _ => notices.Add("view");
            mirror.EventReceived += item => notices.Add("event:" + item.KindText);
            mirror.PhaseChanged += change => notices.Add($"phase:{change.Previous}->{change.Current}");
            mirror.PriorityChanged += change => notices.Add($"priority:{change.Previous?.Value}->{change.Current?.Value}");
            mirror.MatchEnded += ending => notices.Add($"ended:{ending.Won}");
        }

        [Test]
        public void AvisosSaemNaOrdemDoContrato()
        {
            mirror.Apply(MirrorViews.Action(), 5, Array.Empty<MatchEvent>());
            notices.Clear();
            MatchUpdateFrame declaration = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-all-events.json");

            mirror.Apply(MirrorViews.Declaration(), 9, declaration.Events.Take(3).ToArray());

            Assert.That(notices, Is.EqualTo(new[] { "view", "event:mulligan_taken", "event:unit_played", "event:spell_cast", "phase:Action->Declaration", "priority:9->7" }));
        }

        [Test]
        public void SemMudancaDeFaseNemPrioridadeSoSaiOEstado()
        {
            mirror.Apply(MirrorViews.Action(), 5, Array.Empty<MatchEvent>());
            notices.Clear();

            mirror.Apply(MirrorViews.Action(), 6, Array.Empty<MatchEvent>());

            Assert.That(notices, Is.EqualTo(new[] { "view" }));
        }

        [Test]
        public void PrimeiroFrameNaoAvisaFaseNemPrioridade()
        {
            mirror.Apply(MirrorViews.Declaration(), 9, Array.Empty<MatchEvent>());

            Assert.That(notices, Is.EqualTo(new[] { "view" }));
        }

        [Test]
        public void PartidaTerminadaAvisaUmaVezComVitoria()
        {
            mirror.Apply(MirrorViews.Declaration(), 9, Array.Empty<MatchEvent>());
            notices.Clear();

            mirror.Apply(MirrorViews.Finished(), 20, Array.Empty<MatchEvent>());
            mirror.Apply(MirrorViews.Finished(), 21, Array.Empty<MatchEvent>());

            Assert.That(notices.Count(notice => notice.StartsWith("ended", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(notices, Does.Contain("ended:True"));
        }

        [Test]
        public void AssinanteQueLancaNaoCalaOsAvisosSeguintes()
        {
            MatchMirror failing = new MatchMirror(log);
            List<string> events = new List<string>();
            PlayerView? seenInsideEvent = null;
            failing.ViewReplaced += _ => throw new InvalidOperationException("quebrado");
            failing.EventReceived += item => { events.Add(item.KindText); seenInsideEvent = failing.Current; };
            IReadOnlyList<MatchEvent> twoEvents = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-all-events.json").Events.Take(2).ToArray();
            PlayerView view = MirrorViews.Action();

            failing.Apply(view, 5, twoEvents);

            Assert.That(events, Is.EqualTo(new[] { "mulligan_taken", "unit_played" }));
            Assert.That(seenInsideEvent, Is.SameAs(view));
            LogField notice = log.Single("match_subscriber_failed").Fields.First(field => field.Name == "notice");
            Assert.That(notice.Value, Is.EqualTo("view_replaced"));
        }
    }
}
