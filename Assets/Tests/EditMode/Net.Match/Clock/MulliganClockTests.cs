#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US3-4, FR-020: relógio do próprio mulligan.</summary>
    public class MulliganClockTests
    {
        private FakeMonotonicClock time = null!;
        private TurnClock clock = null!;

        [SetUp]
        public void CreateClock()
        {
            time = new FakeMonotonicClock();
            clock = new TurnClock(time, new FakeClientLog());
        }

        [Test]
        public void ContaDoPrazoDoMulliganSemVez()
        {
            clock.Anchor(new ClockView(null, 30000), time.Now).Raise();

            time.Advance(TimeSpan.FromSeconds(12));

            Assert.That(clock.MulliganRemaining, Is.EqualTo(TimeSpan.FromSeconds(18)));
            Assert.That(clock.Turn, Is.Null);
        }

        [Test]
        public void PrazoNuloApagaORelogioDeMulligan()
        {
            clock.Anchor(new ClockView(null, 30000), time.Now).Raise();

            clock.Anchor(ClockView.Empty, time.Now).Raise();

            Assert.That(clock.MulliganRemaining, Is.Null);
        }

        [Test]
        public void MulliganNuncaFicaNegativo()
        {
            clock.Anchor(new ClockView(null, 1000), time.Now).Raise();

            time.Advance(TimeSpan.FromMinutes(5));

            Assert.That(clock.MulliganRemaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void SemAncoraNaoHaRelogio()
        {
            Assert.That((clock.MulliganRemaining, clock.TurnRemaining), Is.EqualTo(((TimeSpan?)null, (TimeSpan?)null)));
        }
    }
}
