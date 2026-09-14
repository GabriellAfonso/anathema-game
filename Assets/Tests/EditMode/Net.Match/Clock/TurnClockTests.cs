#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US3-1, US3-2, US3-7, FR-017, FR-018, FR-022: contagem, vez nova e nada em zero.</summary>
    public class TurnClockTests
    {
        private static readonly UserId Rival = new UserId(9);

        private FakeMonotonicClock time = null!;
        private TurnClock clock = null!;
        private List<string> notices = null!;

        [SetUp]
        public void CreateClock()
        {
            time = new FakeMonotonicClock();
            clock = new TurnClock(time, new FakeClientLog());
            notices = new List<string>();
            clock.TurnStarted += turn => notices.Add($"started:{turn.TurnNumber}:{turn.Holder.Value}");
            clock.TurnRunningOut += turnNumber => notices.Add($"running_out:{turnNumber}");
        }

        [Test]
        public void RestanteEhOMedidoMenosODecorridoEParaEmZero()
        {
            clock.Anchor(Turn(12, 25000), time.Now).Raise();
            notices.Clear();

            time.Advance(TimeSpan.FromMilliseconds(3000));
            Assert.That(clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(22000)));
            Assert.That(clock.Turn!.Holder, Is.EqualTo(Rival));

            time.Advance(TimeSpan.FromMilliseconds(27000));
            Assert.That(clock.TurnRemaining, Is.EqualTo(TimeSpan.Zero));
            Assert.That(notices, Is.Empty);
        }

        [Test]
        public void VezNovaAvisaComNumeroEDono()
        {
            clock.Anchor(Turn(12, 25000), time.Now).Raise();

            clock.Anchor(Turn(13, 30000), time.Now).Raise();

            Assert.That(notices, Is.EqualTo(new[] { "started:12:9", "started:13:9" }));
        }

        [Test]
        public void MesmaVezComRestanteMenorSoReancora()
        {
            clock.Anchor(Turn(12, 25000), time.Now).Raise();
            time.Advance(TimeSpan.FromMilliseconds(4000));

            clock.Anchor(Turn(12, 20000), time.Now).Raise();
            time.Advance(TimeSpan.FromMilliseconds(1000));

            Assert.That(notices, Is.EqualTo(new[] { "started:12:9" }));
            Assert.That(clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(19000)));
        }

        [Test]
        public void AvisosSoSaemNoRaise()
        {
            ClockAnnouncements pending = clock.Anchor(new ClockView(new TurnView(12, Rival, 10000, true), null), time.Now);

            Assert.That(notices, Is.Empty);
            Assert.That(clock.Turn!.TurnNumber, Is.EqualTo(12));
            pending.Raise();
            Assert.That(notices, Is.EqualTo(new[] { "started:12:9", "running_out:12" }));
        }

        [Test]
        public void VezNulaApagaSemAviso()
        {
            clock.Anchor(Turn(12, 25000), time.Now).Raise();
            notices.Clear();

            clock.Anchor(ClockView.Empty, time.Now).Raise();

            Assert.That((clock.Turn, clock.TurnRemaining), Is.EqualTo(((TurnView?)null, (TimeSpan?)null)));
            Assert.That(notices, Is.Empty);
        }

        [Test]
        public void MesmoDonoNaRodadaSeguinteEhVezNova()
        {
            clock.Anchor(Turn(12, 25000), time.Now).Raise();

            clock.Anchor(new ClockView(new TurnView(14, Rival, 30000, false), null), time.Now).Raise();

            Assert.That(notices, Is.EqualTo(new[] { "started:12:9", "started:14:9" }));
        }

        private static ClockView Turn(long number, long remainingMs) => new ClockView(new TurnView(number, Rival, remainingMs, false), null);
    }
}
