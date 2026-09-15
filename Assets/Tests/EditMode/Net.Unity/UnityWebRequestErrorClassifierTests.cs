#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class UnityWebRequestErrorClassifierTests
    {
        // Categoria esperada por nome: TransportFailureKind é interno desde a 005 e teste público não recebe tipo interno por parâmetro.
        [TestCase("Request timeout", "Timeout")]
        [TestCase("Cannot resolve destination host", "HostNotResolved")]
        [TestCase("Cannot connect to destination host", "CannotConnect")]
        [TestCase("Unable to complete SSL connection", "Other")]
        [TestCase(null, "Other")]
        public void TextoDoUnityViraCategoria(string? unityError, string expectedName)
        {
            Assert.That(UnityWebRequestErrorClassifier.Classify(unityError).ToString(), Is.EqualTo(expectedName));
        }
    }
}
