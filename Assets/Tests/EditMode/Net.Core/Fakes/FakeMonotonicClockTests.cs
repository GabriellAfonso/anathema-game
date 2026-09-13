#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeMonotonicClockTests
    {
        [Test]
        public void ComecaLongeDoZero()
        {
            Assert.That(new FakeMonotonicClock().Now.Ticks, Is.EqualTo(FakeMonotonicClock.StartTicks).And.Not.Zero);
        }

        [Test]
        public void AdvanceSomaExatamente()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            MonotonicInstant start = clock.Now;

            clock.Advance(TimeSpan.FromMinutes(7));

            Assert.That(clock.Now - start, Is.EqualTo(TimeSpan.FromMinutes(7)));
        }

        [Test]
        public void AdvanceNegativoLanca()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(TimeSpan.FromTicks(-1)));
        }
    }
}
