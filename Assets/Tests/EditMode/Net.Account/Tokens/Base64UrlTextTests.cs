#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class Base64UrlTextTests
    {
        // "YWJj" = "abc" (resto 0), "YWI" = "ab" (resto 3), "YQ" = "a" (resto 2);
        // "fn5-" e "Pz8_" usam os dois caracteres que só existem no base64url.
        [TestCase("YWJj", "abc")]
        [TestCase("YWI", "ab")]
        [TestCase("YQ", "a")]
        [TestCase("fn5-", "~~~")]
        [TestCase("Pz8_", "???")]
        public void DecodificaSemPreenchimento(string encoded, string expected)
        {
            bool decoded = Base64UrlText.TryDecode(encoded, out string text, out string problem);

            Assert.That(decoded, Is.True, problem);
            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void ComprimentoQuatroNMaisUmFalha()
        {
            bool decoded = Base64UrlText.TryDecode("abcde", out _, out string problem);

            Assert.That(decoded, Is.False);
            Assert.That(problem, Does.Contain("5"));
        }

        [Test]
        public void CaractereForaDoAlfabetoFalhaSemEcoarOTexto()
        {
            bool decoded = Base64UrlText.TryDecode("ab*d", out _, out string problem);

            Assert.That(decoded, Is.False);
            Assert.That(problem, Does.Not.Contain("ab*d"));
        }
    }
}
