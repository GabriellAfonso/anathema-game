#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountTimingTests
    {
        [Test]
        public void MargemPadraoEhTrintaSegundos()
        {
            Assert.That(new AccountTiming().RenewalMargin, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [TestCase(-1)]
        [TestCase(121)]
        public void MargemForaDeZeroACentoEVinteLancaComOValor(int seconds)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new AccountTiming(TimeSpan.FromSeconds(seconds)));

            Assert.That(error.Message, Does.Contain(seconds + "s"));
        }

        [TestCase(0)]
        [TestCase(120)]
        public void MargemNosLimitesEhAceita(int seconds)
        {
            Assert.That(new AccountTiming(TimeSpan.FromSeconds(seconds)).RenewalMargin.TotalSeconds, Is.EqualTo(seconds));
        }
    }
}
