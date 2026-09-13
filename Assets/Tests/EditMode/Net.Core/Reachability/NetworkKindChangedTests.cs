#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class NetworkKindChangedTests
    {
        [Test]
        public void MudancaSemMudarLanca()
        {
            Assert.Throws<ArgumentException>(() => new NetworkKindChanged(NetworkKind.LocalArea, NetworkKind.LocalArea));
        }

        [Test]
        public void WifiParaDadosMoveisEhAceito()
        {
            NetworkKindChanged change = new NetworkKindChanged(NetworkKind.LocalArea, NetworkKind.CarrierData);

            Assert.That(change.Previous, Is.EqualTo(NetworkKind.LocalArea));
            Assert.That(change.Current, Is.EqualTo(NetworkKind.CarrierData));
        }
    }
}
