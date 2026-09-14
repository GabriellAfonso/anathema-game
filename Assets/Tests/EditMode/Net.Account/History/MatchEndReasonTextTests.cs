#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class MatchEndReasonTextTests
    {
        [TestCase("nexus_depleted", MatchEndReason.NexusDepleted)]
        [TestCase("forfeit", MatchEndReason.Forfeit)]
        public void TextoConhecidoViraOMotivo(string text, MatchEndReason expected)
        {
            Assert.That(MatchEndReasonText.Parse(text), Is.EqualTo(expected));
        }

        [TestCase("timeout")]
        [TestCase("")]
        [TestCase("Forfeit")]
        public void TextoForaDoConjuntoEhDesconhecido(string text)
        {
            Assert.That(MatchEndReasonText.Parse(text), Is.EqualTo(MatchEndReason.Unknown));
        }

        [Test]
        public void TextoNuloLanca()
        {
            Assert.Throws<ArgumentNullException>(() => MatchEndReasonText.Parse(null!));
        }
    }
}
