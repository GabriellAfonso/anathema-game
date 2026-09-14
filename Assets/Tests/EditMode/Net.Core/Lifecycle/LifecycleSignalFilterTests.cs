#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class LifecycleSignalFilterTests
    {
        private FakeMonotonicClock clock = null!;
        private List<string> signals = null!;
        private TimeSpan lastAwayFor;

        [SetUp]
        public void CreateClockAndRecorder()
        {
            clock = new FakeMonotonicClock();
            signals = new List<string>();
        }

        [Test]
        public void PausaLevaAoSegundoPlanoEVoltaTrazADuracao()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: false);

            filter.OnPause(true);
            clock.Advance(TimeSpan.FromMinutes(7));
            filter.OnPause(false);

            Assert.That(signals, Is.EqualTo(new[] { "background", "foreground" }));
            Assert.That(lastAwayFor, Is.EqualTo(TimeSpan.FromMinutes(7)));
        }

        [Test]
        public void FocoDepoisDaPausaTambemContaComoVolta()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: false);

            filter.OnPause(true);
            filter.OnFocus(true);

            Assert.That(signals, Is.EqualTo(new[] { "background", "foreground" }));
        }

        [Test]
        public void PerdaDeFocoSemPararOPlayerEhIgnorada()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: false);

            filter.OnFocus(false);
            filter.OnFocus(true);

            Assert.That(signals, Is.Empty);
        }

        [Test]
        public void PerdaDeFocoQueParaOPlayerLevaAoSegundoPlano()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: true);

            filter.OnFocus(false);
            filter.OnFocus(true);

            Assert.That(signals, Is.EqualTo(new[] { "background", "foreground" }));
        }

        [Test]
        public void SinaisRepetidosNaoRepetemAvisos()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: true);

            filter.OnFocus(false);
            filter.OnPause(true);
            filter.OnPause(false);
            filter.OnFocus(true);

            Assert.That(signals, Is.EqualTo(new[] { "background", "foreground" }));
        }

        [Test]
        public void VoltaSemIdaNaoAvisa()
        {
            LifecycleSignalFilter filter = Filter(focusLossStopsPlayer: false);

            filter.OnPause(false);

            Assert.That(signals, Is.Empty);
        }

        private LifecycleSignalFilter Filter(bool focusLossStopsPlayer)
        {
            LifecycleSignalFilter filter = new LifecycleSignalFilter(clock, focusLossStopsPlayer);
            filter.WentToBackground += _ => signals.Add("background");
            filter.ReturnedToForeground += signal =>
            {
                signals.Add("foreground");
                lastAwayFor = signal.AwayFor;
            };
            return filter;
        }

        [Test]
        public void ModoAndroidIgnoraPerdaDeFocoSozinha()
        {
            List<string> seen = new List<string>();
            LifecycleSignalFilter filter = RecordInto(new LifecycleSignalFilter(clock, BackgroundSignalMode.AndroidPause), seen);

            filter.OnFocus(false);
            filter.OnPause(true);
            filter.OnFocus(true);

            Assert.That(seen, Is.EqualTo(new[] { "background", "foreground" }));
        }

        [Test]
        public void ModoDesktopQueParaNoFocoLevaAoSegundoPlano()
        {
            List<string> seen = new List<string>();
            LifecycleSignalFilter filter = RecordInto(new LifecycleSignalFilter(clock, BackgroundSignalMode.DesktopStopsOnFocusLoss), seen);

            filter.OnFocus(false);
            filter.OnFocus(true);

            Assert.That(seen, Is.EqualTo(new[] { "background", "foreground" }));
        }

        [Test]
        public void ModoDesktopQueContinuaRodandoNuncaVaiAoSegundoPlano()
        {
            List<string> seen = new List<string>();
            LifecycleSignalFilter filter = RecordInto(new LifecycleSignalFilter(clock, BackgroundSignalMode.DesktopKeepsRunning), seen);

            filter.OnPause(true);
            filter.OnFocus(false);
            filter.OnPause(false);
            filter.OnFocus(true);

            Assert.That(seen, Is.Empty);
        }

        private static LifecycleSignalFilter RecordInto(LifecycleSignalFilter filter, List<string> seen)
        {
            filter.WentToBackground += _ => seen.Add("background");
            filter.ReturnedToForeground += _ => seen.Add("foreground");
            return filter;
        }
    }
}
