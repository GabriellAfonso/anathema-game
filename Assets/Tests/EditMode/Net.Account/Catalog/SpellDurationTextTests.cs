#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class SpellDurationTextTests
    {
        [TestCase("permanent", SpellDuration.Permanent)]
        [TestCase("until_end_of_round", SpellDuration.UntilEndOfRound)]
        public void TextoConhecidoViraADuracao(string text, SpellDuration expected)
        {
            Assert.That(SpellDurationText.Parse(text), Is.EqualTo(expected));
        }

        [TestCase("until_end_of_turn")]
        [TestCase("")]
        public void TextoForaDoConjuntoEhDesconhecido(string text)
        {
            Assert.That(SpellDurationText.Parse(text), Is.EqualTo(SpellDuration.Unknown));
        }

        [Test]
        public void TextoNuloLanca()
        {
            Assert.Throws<ArgumentNullException>(() => SpellDurationText.Parse(null!));
        }
    }
}
