#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US3-3, FR-019: aviso de tempo acabando, uma vez por vez, vindo do frame ou de <c>turn_warning</c>.</summary>
    public class TurnWarningTests
    {
        private static readonly UserId Self = new UserId(7);

        private FakeMonotonicClock time = null!;
        private FakeClientLog log = null!;
        private TurnClock clock = null!;
        private List<long> runningOut = null!;

        [SetUp]
        public void CreateClock()
        {
            time = new FakeMonotonicClock();
            log = new FakeClientLog();
            clock = new TurnClock(time, log);
            runningOut = new List<long>();
            clock.TurnRunningOut += runningOut.Add;
        }

        [Test]
        public void AvisoDaVezDesenhadaSaiUmaVez()
        {
            DrawTurn(12, false);

            clock.NoteWarning(Warning(12), time.Now);
            clock.NoteWarning(Warning(12), time.Now);

            Assert.That(runningOut, Is.EqualTo(new[] { 12L }));
        }

        [Test]
        public void AvisoDeOutraVezEhIgnoradoERegistrado()
        {
            DrawTurn(12, false);

            clock.NoteWarning(Warning(11), time.Now);

            Assert.That(runningOut, Is.Empty);
            Assert.That(clock.Turn!.RemainingMs, Is.EqualTo(25000));
            Assert.That(log.Single("turn_warning_ignored").Level, Is.EqualTo(ClientLogLevel.Debug));
        }

        [Test]
        public void AvisoSemVezDesenhadaEhIgnorado()
        {
            clock.NoteWarning(Warning(12), time.Now);

            Assert.That((runningOut.Count, clock.Turn), Is.EqualTo((0, (TurnView?)null)));
        }

        [Test]
        public void AvisoDaVezReancoraComORestanteDele()
        {
            DrawTurn(12, false);
            time.Advance(TimeSpan.FromSeconds(12));

            clock.NoteWarning(Warning(12), time.Now);
            time.Advance(TimeSpan.FromSeconds(2));

            Assert.That(clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(13000)));
            Assert.That(clock.Turn!.Warning, Is.True);
        }

        [Test]
        public void WarningDoFrameAvisaENaoRepeteComTurnWarning()
        {
            DrawTurn(12, true);

            clock.NoteWarning(Warning(12), time.Now);
            clock.Anchor(new ClockView(new TurnView(12, Self, 9000, true), null), time.Now).Raise();

            Assert.That(runningOut, Is.EqualTo(new[] { 12L }));
        }

        [Test]
        public void VezNovaPodeAvisarDeNovo()
        {
            DrawTurn(12, true);

            DrawTurn(13, true);

            Assert.That(runningOut, Is.EqualTo(new[] { 12L, 13L }));
        }

        private void DrawTurn(long number, bool warning)
        {
            clock.Anchor(new ClockView(new TurnView(number, Self, 25000, warning), null), time.Now).Raise();
        }

        private static TurnWarningFrame Warning(long turnNumber) => new TurnWarningFrame(turnNumber, Self, 15000);
    }
}
