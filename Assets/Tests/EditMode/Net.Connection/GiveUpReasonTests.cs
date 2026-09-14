#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class GiveUpReasonTests
    {
        private static IEnumerable<GiveUpReason> AllReasons()
        {
            yield return GiveUpReason.NoSession();
            yield return GiveUpReason.SessionExpired();
            yield return GiveUpReason.TokenRefusedRepeatedly(3);
            yield return GiveUpReason.AttemptsExhausted(5);
            foreach (MatchRefusalDetail detail in Enum.GetValues(typeof(MatchRefusalDetail)))
                yield return GiveUpReason.MatchRefused(detail);
        }

        [Test]
        public void TodoMotivoTemTextoProprioParaOJogador()
        {
            string[] texts = AllReasons().Select(reason => reason.PlayerText()).ToArray();

            Assert.That(texts, Has.None.Empty);
            Assert.That(texts.Distinct().Count(), Is.EqualTo(texts.Length));
        }

        [Test]
        public void TodoGiveUpKindTemFabrica()
        {
            GiveUpKind[] covered = AllReasons().Select(reason => reason.Kind).Distinct().ToArray();

            Assert.That(covered, Is.EquivalentTo(Enum.GetValues(typeof(GiveUpKind))));
        }

        [Test]
        public void ContagemSoNosMotivosQueContam()
        {
            Assert.That(GiveUpReason.AttemptsExhausted(5).Attempts, Is.EqualTo(5));
            Assert.That(GiveUpReason.TokenRefusedRepeatedly(3).Attempts, Is.EqualTo(3));
            Assert.That(GiveUpReason.SessionExpired().Attempts, Is.Null);
            Assert.That(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound).Attempts, Is.Null);
            Assert.That(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound).Match, Is.EqualTo(MatchRefusalDetail.MatchNotFound));
        }

        [Test]
        public void ContagemMenorQueUmLanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GiveUpReason.AttemptsExhausted(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GiveUpReason.TokenRefusedRepeatedly(0));
        }
    }
}
