#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class MatchIdTests
    {
        private const string Uuid = "0b7c2f4e-1d2a-4c55-9a8e-3f1b6d7e9c10";

        [Test]
        public void IgualdadeOrdinal()
        {
            Assert.That(new MatchId(Uuid) == new MatchId(Uuid), Is.True);
            Assert.That(new MatchId(Uuid) == new MatchId(Uuid.ToUpperInvariant()), Is.False);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" " + Uuid)]
        [TestCase(Uuid + " ")]
        public void VazioOuComEspacoNasPontasLanca(string value)
        {
            Assert.Throws<ArgumentException>(() => new MatchId(value));
        }

        [Test]
        public void ToStringNomeiaOCampo()
        {
            Assert.That(new MatchId(Uuid).ToString(), Is.EqualTo("match_id=" + Uuid));
        }
    }
}
