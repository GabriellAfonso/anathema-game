#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class NetworkKindTrackerTests
    {
        [Test]
        public void NaoConsultaAntesDeUmSegundo()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            NetworkKindTracker tracker = new NetworkKindTracker(clock, NetworkKind.LocalArea);

            clock.Advance(TimeSpan.FromMilliseconds(999));

            Assert.That(tracker.ShouldPoll(), Is.False);
        }

        [Test]
        public void ConsultaDepoisDeUmSegundoERecomecaAContagem()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            NetworkKindTracker tracker = new NetworkKindTracker(clock, NetworkKind.LocalArea);

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.That(tracker.ShouldPoll(), Is.True);
            Assert.That(tracker.ShouldPoll(), Is.False);
        }

        [Test]
        public void WifiParaDadosMoveisAvisaUmaVez()
        {
            NetworkKindTracker tracker = new NetworkKindTracker(new FakeMonotonicClock(), NetworkKind.LocalArea);
            List<NetworkKindChanged> changes = new List<NetworkKindChanged>();
            tracker.Changed += changes.Add;

            tracker.Observe(NetworkKind.CarrierData);
            tracker.Observe(NetworkKind.CarrierData);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Previous, Is.EqualTo(NetworkKind.LocalArea));
            Assert.That(tracker.Current, Is.EqualTo(NetworkKind.CarrierData));
        }

        [Test]
        public void MesmoValorNaoAvisa()
        {
            NetworkKindTracker tracker = new NetworkKindTracker(new FakeMonotonicClock(), NetworkKind.None);
            int changes = 0;
            tracker.Changed += _ => changes++;

            tracker.Observe(NetworkKind.None);

            Assert.That(changes, Is.Zero);
        }
    }
}
