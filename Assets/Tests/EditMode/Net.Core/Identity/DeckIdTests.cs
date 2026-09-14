#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class DeckIdTests
    {
        [Test]
        public void IgualdadePorValor()
        {
            Assert.That(new DeckId(4) == new DeckId(4), Is.True);
            Assert.That(new DeckId(4).Equals(new DeckId(5)), Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ValorMenorQueUmLancaComOValor(long value)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DeckId(value));

            Assert.That(error.Message, Does.Contain(value.ToString()));
        }

        [Test]
        public void ToStringNomeiaOCampo()
        {
            Assert.That(new DeckId(4).ToString(), Is.EqualTo("deck_id=4"));
        }
    }
}
