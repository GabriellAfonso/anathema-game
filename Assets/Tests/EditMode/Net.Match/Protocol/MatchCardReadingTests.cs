#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-004: carta e perfil tipados.</summary>
    public class MatchCardReadingTests
    {
        [Test]
        public void CartaTemCopiaEEntradaDoCatalogo()
        {
            MatchCard card = MatchCard.Read(MatchJson.Reader("{\"card_instance_id\": 21, \"card_id\": 5}"));

            Assert.That(card, Is.EqualTo(new MatchCard(new CardInstanceId(21), new CardId(5))));
        }

        [Test]
        public void CartaSemCardIdFalhaComCaminho()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => MatchCard.Read(MatchJson.Reader("{\"card_instance_id\": 21}")));

            Assert.That(error.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(error.Failure.Path, Does.EndWith("card_id"));
        }

        [Test]
        public void PerfilTemOsQuatroCampos()
        {
            MatchProfile profile = MatchProfile.Read(MatchJson.Reader("{\"user_id\": 7, \"nickname\": \"gabriel\", \"icon\": \"default\", \"level\": 2}"));

            Assert.That((profile.User, profile.Nickname, profile.Icon, profile.Level), Is.EqualTo((new UserId(7), "gabriel", "default", 2L)));
        }
    }
}
