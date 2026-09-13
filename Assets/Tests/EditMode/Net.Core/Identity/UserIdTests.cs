#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class UserIdTests
    {
        [Test]
        public void IgualdadePorValor()
        {
            Assert.That(new UserId(7) == new UserId(7), Is.True);
            Assert.That(new UserId(7).Equals(new UserId(9)), Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ValorMenorQueUmLanca(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UserId(value));
        }

        [Test]
        public void ToStringNomeiaOCampo()
        {
            Assert.That(new UserId(7).ToString(), Is.EqualTo("user_id=7"));
        }
    }
}
