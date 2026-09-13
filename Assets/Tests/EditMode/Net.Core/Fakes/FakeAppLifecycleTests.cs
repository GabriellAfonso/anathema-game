#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeAppLifecycleTests
    {
        [Test]
        public void SeteMinutosForaChegamNaVolta()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            FakeAppLifecycle lifecycle = new FakeAppLifecycle(clock);
            WentToBackground? left = null;
            ReturnedToForeground? returned = null;
            lifecycle.WentToBackground += signal => left = signal;
            lifecycle.ReturnedToForeground += signal => returned = signal;

            lifecycle.SimulateBackground();
            clock.Advance(TimeSpan.FromMinutes(7));
            lifecycle.SimulateForeground();

            Assert.That(left, Is.Not.Null);
            Assert.That(returned!.AwayFor, Is.EqualTo(TimeSpan.FromMinutes(7)));
        }

        [Test]
        public void VoltarSemIrLanca()
        {
            FakeAppLifecycle lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());

            Assert.Throws<InvalidOperationException>(lifecycle.SimulateForeground);
        }

        [Test]
        public void IrDuasVezesLanca()
        {
            FakeAppLifecycle lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());
            lifecycle.SimulateBackground();

            Assert.Throws<InvalidOperationException>(lifecycle.SimulateBackground);
        }
    }
}
