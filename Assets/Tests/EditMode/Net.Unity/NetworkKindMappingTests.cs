#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using UnityEngine;

namespace Anathema.Net.Unity.Tests
{
    public class NetworkKindMappingTests
    {
        // Tipo esperado por nome: NetworkKind é interno desde a 005 e teste público não recebe tipo interno por parâmetro.
        [TestCase(NetworkReachability.NotReachable, "None")]
        [TestCase(NetworkReachability.ReachableViaLocalAreaNetwork, "LocalArea")]
        [TestCase(NetworkReachability.ReachableViaCarrierDataNetwork, "CarrierData")]
        public void CadaValorDoUnityTemSeuTipo(NetworkReachability unity, string expectedName)
        {
            Assert.That(NetworkKindMapping.From(unity).ToString(), Is.EqualTo(expectedName));
        }
    }
}
