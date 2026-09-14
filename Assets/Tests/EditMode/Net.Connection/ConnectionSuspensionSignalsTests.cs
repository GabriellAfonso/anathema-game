#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionSuspensionSignalsTests
    {
        private FakeAppLifecycle lifecycle = null!;
        private FakeNetworkReachability reachability = null!;
        private List<string> signals = null!;

        [SetUp]
        public void CreateSignals()
        {
            lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());
            reachability = new FakeNetworkReachability(NetworkKind.LocalArea);
            signals = new List<string>();
        }

        [Test]
        public void ComecaSemRedeSeARedeJaEstaAusente()
        {
            ConnectionSuspension suspension = new ConnectionSuspension(lifecycle, new FakeNetworkReachability(NetworkKind.None));

            Assert.That(suspension.IsSuspended, Is.True);
            Assert.That(suspension.Reason, Is.EqualTo(SuspensionReason.NoNetwork));
        }

        [Test]
        public void SegundoPlanoGanhaDeSemRedeNoMotivo()
        {
            ConnectionSuspension suspension = Record();

            lifecycle.SimulateBackground();
            reachability.SimulateKind(NetworkKind.None);
            Assert.That(suspension.Reason, Is.EqualTo(SuspensionReason.Background));
            lifecycle.SimulateForeground();

            Assert.That(suspension.Reason, Is.EqualTo(SuspensionReason.NoNetwork));
            Assert.That(signals, Is.EqualTo(new[] { "suspended", "suspended", "Foreground" }));
        }

        [Test]
        public void RedeDeVoltaETrocaDeTipoSaoCausasDiferentes()
        {
            ConnectionSuspension suspension = Record();

            reachability.SimulateKind(NetworkKind.CarrierData);
            reachability.SimulateKind(NetworkKind.None);
            reachability.SimulateKind(NetworkKind.LocalArea);

            Assert.That(signals, Is.EqualTo(new[] { "NetworkKindChanged", "suspended", "NetworkBack" }));
            Assert.That(suspension.IsSuspended, Is.False);
            Assert.That(suspension.LastChange!.Previous, Is.EqualTo(NetworkKind.None));
        }

        [Test]
        public void DisposeParaDeOuvir()
        {
            ConnectionSuspension suspension = Record();

            suspension.Dispose();
            lifecycle.SimulateBackground();
            reachability.SimulateKind(NetworkKind.None);

            Assert.That(signals, Is.Empty);
            Assert.That(suspension.IsSuspended, Is.False);
        }

        private ConnectionSuspension Record()
        {
            ConnectionSuspension suspension = new ConnectionSuspension(lifecycle, reachability);
            suspension.Suspended += () => signals.Add("suspended");
            suspension.Resumed += cause => signals.Add(cause.ToString());
            return suspension;
        }
    }
}
