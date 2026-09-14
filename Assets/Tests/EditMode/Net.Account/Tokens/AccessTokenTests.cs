#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccessTokenTests
    {
        private static readonly MonotonicInstant Arrived = new MonotonicInstant(TimeSpan.FromHours(1).Ticks);
        private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);

        [TestCase(0, false)]
        [TestCase(269, false)]
        [TestCase(270, true)]
        [TestCase(271, true)]
        [TestCase(400, true)]
        public void PrecisaRenovarQuandoFaltaMenosQueAMargem(int secondsAfterArrival, bool expected)
        {
            AccessToken token = FiveMinuteToken("secret-token-text");

            Assert.That(token.NeedsRenewal(Arrived.Add(TimeSpan.FromSeconds(secondsAfterArrival)), Margin), Is.EqualTo(expected));
        }

        [Test]
        public void SemMargemVenceExatamenteNoFimDaVida()
        {
            AccessToken token = FiveMinuteToken("secret-token-text");

            Assert.That(token.NeedsRenewal(Arrived.Add(TimeSpan.FromSeconds(299)), TimeSpan.Zero), Is.False);
            Assert.That(token.NeedsRenewal(Arrived.Add(TimeSpan.FromSeconds(300)), TimeSpan.Zero), Is.True);
        }

        [Test]
        public void ExpiraNaChegadaMaisAVida()
        {
            Assert.That(FiveMinuteToken("secret-token-text").ExpiresAt, Is.EqualTo(Arrived.Add(TimeSpan.FromMinutes(5))));
        }

        [Test]
        public void ToStringNaoMostraOTexto()
        {
            string text = FiveMinuteToken("secret-token-text").ToString();

            Assert.That(text, Does.Contain("<redacted>").And.Contain("user_id=7"));
            Assert.That(text, Does.Not.Contain("secret-token-text"));
        }

        private static AccessToken FiveMinuteToken(string text)
        {
            return new AccessToken(text, new UserId(7), TimeSpan.FromMinutes(5), Arrived);
        }
    }
}
