#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using UnityEngine;

namespace Anathema.Net.Unity.Tests
{
    public class NetworkKindMappingTests
    {
        [TestCase(NetworkReachability.NotReachable, NetworkKind.None)]
        [TestCase(NetworkReachability.ReachableViaLocalAreaNetwork, NetworkKind.LocalArea)]
        [TestCase(NetworkReachability.ReachableViaCarrierDataNetwork, NetworkKind.CarrierData)]
        public void CadaValorDoUnityTemSeuTipo(NetworkReachability unity, NetworkKind expected)
        {
            Assert.That(NetworkKindMapping.From(unity), Is.EqualTo(expected));
        }
    }
}
