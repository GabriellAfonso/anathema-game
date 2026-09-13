#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class UnityAppLifecycleTests
    {
        [Test]
        public void AvisosSaemPelaFila()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            MainThreadQueue queue = new MainThreadQueue(new FakeClientLog());
            UnityAppLifecycle lifecycle = new UnityAppLifecycle(new LifecycleSignalFilter(clock, false), queue);
            int background = 0;
            TimeSpan? awayFor = null;
            lifecycle.WentToBackground += _ => background++;
            lifecycle.ReturnedToForeground += signal => awayFor = signal.AwayFor;

            lifecycle.OnPause(true);
            Assert.That(background, Is.Zero);
            queue.Drain();
            clock.Advance(TimeSpan.FromMinutes(6));
            lifecycle.OnPause(false);
            queue.Drain();

            Assert.That(background, Is.EqualTo(1));
            Assert.That(awayFor, Is.EqualTo(TimeSpan.FromMinutes(6)));
        }

        [Test]
        public void FocoSemPausaNoAndroidNaoAvisa()
        {
            MainThreadQueue queue = new MainThreadQueue(new FakeClientLog());
            UnityAppLifecycle lifecycle = new UnityAppLifecycle(new LifecycleSignalFilter(new FakeMonotonicClock(), false), queue);
            int signals = 0;
            lifecycle.WentToBackground += _ => signals++;

            lifecycle.OnFocus(false);
            queue.Drain();

            Assert.That(signals, Is.Zero);
        }
    }
}
