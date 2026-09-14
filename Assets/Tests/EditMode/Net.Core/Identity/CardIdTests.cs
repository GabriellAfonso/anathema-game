#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class CardIdTests
    {
        [Test]
        public void IgualdadePorValor()
        {
            Assert.That(new CardId(1004) == new CardId(1004), Is.True);
            Assert.That(new CardId(1004).Equals(new CardId(1)), Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ValorMenorQueUmLancaComOValor(long value)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new CardId(value));

            Assert.That(error.Message, Does.Contain(value.ToString()));
        }

        [Test]
        public void ToStringNomeiaOCampo()
        {
            Assert.That(new CardId(1004).ToString(), Is.EqualTo("card_id=1004"));
        }
    }
}
