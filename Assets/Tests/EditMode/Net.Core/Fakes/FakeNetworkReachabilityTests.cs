#nullable enable
using System.Collections.Generic;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeNetworkReachabilityTests
    {
        [Test]
        public void WifiParaDadosMoveisAvisaUmaVez()
        {
            FakeNetworkReachability reachability = new FakeNetworkReachability(NetworkKind.LocalArea);
            List<NetworkKindChanged> changes = new List<NetworkKindChanged>();
            reachability.Changed += changes.Add;

            reachability.SimulateKind(NetworkKind.CarrierData);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].Previous, Is.EqualTo(NetworkKind.LocalArea));
            Assert.That(changes[0].Current, Is.EqualTo(NetworkKind.CarrierData));
            Assert.That(reachability.Current, Is.EqualTo(NetworkKind.CarrierData));
        }

        [Test]
        public void RepetirOTipoAtualNaoAvisa()
        {
            FakeNetworkReachability reachability = new FakeNetworkReachability(NetworkKind.None);
            int changes = 0;
            reachability.Changed += _ => changes++;

            reachability.SimulateKind(NetworkKind.None);

            Assert.That(changes, Is.Zero);
        }
    }
}
