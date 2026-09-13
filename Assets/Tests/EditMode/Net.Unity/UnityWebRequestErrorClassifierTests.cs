#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class UnityWebRequestErrorClassifierTests
    {
        [TestCase("Request timeout", TransportFailureKind.Timeout)]
        [TestCase("Cannot resolve destination host", TransportFailureKind.HostNotResolved)]
        [TestCase("Cannot connect to destination host", TransportFailureKind.CannotConnect)]
        [TestCase("Unable to complete SSL connection", TransportFailureKind.Other)]
        [TestCase(null, TransportFailureKind.Other)]
        public void TextoDoUnityViraCategoria(string? unityError, TransportFailureKind expected)
        {
            Assert.That(UnityWebRequestErrorClassifier.Classify(unityError), Is.EqualTo(expected));
        }
    }
}
