#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class CardInstanceIdTests
    {
        [Test]
        public void ZeroEhValido()
        {
            Assert.That(new CardInstanceId(0).Value, Is.Zero);
        }

        [Test]
        public void NegativoLanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardInstanceId(-1));
        }

        [Test]
        public void IgualdadePorValorEToString()
        {
            Assert.That(new CardInstanceId(3) == new CardInstanceId(3), Is.True);
            Assert.That(new CardInstanceId(3).ToString(), Is.EqualTo("card_instance_id=3"));
        }
    }
}
