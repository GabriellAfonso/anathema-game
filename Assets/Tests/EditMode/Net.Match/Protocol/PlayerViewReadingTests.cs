#nullable enable
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US1-1, US1-6, FR-002: a visão completa, com os nulos do mulligan, combate e desfecho.</summary>
    public class PlayerViewReadingTests
    {
        [Test]
        public void MulliganTemOsDoisLadosEOsNulos()
        {
            PlayerView view = MatchJson.Fixture<MatchStartFrame>("contract-match-start-mulligan.json").View;

            Assert.That((view.Match, view.RoundNumber, view.Phase), Is.EqualTo((new MatchId("match-7"), 1L, MatchPhase.Mulligan)));
            Assert.That(view.PriorityUser, Is.Null);
            Assert.That(view.TokenHolder, Is.Null);
            Assert.That(view.Combat, Is.Null);
            Assert.That(view.Outcome, Is.Null);
            Assert.That((view.TokenConsumed, view.ConsecutivePasses), Is.EqualTo((false, 0L)));
        }

        [Test]
        public void ProprioLadoTemMaoTipadaEOponenteSoOTamanho()
        {
            PlayerView view = MatchJson.Fixture<MatchStartFrame>("contract-match-start-mulligan.json").View;

            Assert.That(view.You.Hand.Select(card => card.Instance), Is.EqualTo(new[] { new CardInstanceId(1), new CardInstanceId(2), new CardInstanceId(3), new CardInstanceId(4) }));
            Assert.That(view.You.Hand[2].Card, Is.EqualTo(new CardId(1002)));
            Assert.That(view.Opponent.HandSize, Is.EqualTo(4));
            Assert.That((view.You.Profile.User, view.You.Profile.Nickname), Is.EqualTo((new UserId(7), "gabriel")));
            Assert.That((view.Opponent.Profile.User, view.Opponent.Profile.Level), Is.EqualTo((new UserId(9), 3L)));
            Assert.That((view.You.Nexus, view.You.EnergyCurrent, view.You.DeckSize), Is.EqualTo((20L, 0L, 36L)));
            Assert.That((view.You.MulliganTaken, view.Opponent.MulliganTaken), Is.EqualTo((false, true)));
        }

        [Test]
        public void BancoECemiterioSaoLidos()
        {
            PlayerView view = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-action.json").View;

            Assert.That(view.You.Bank.Single().Card, Is.EqualTo(new MatchCard(new CardInstanceId(21), new CardId(2))));
            Assert.That(view.Opponent.Bank.Single().DamageTaken, Is.EqualTo(1));
            Assert.That(view.You.Graveyard.Single().Card, Is.EqualTo(new CardId(1002)));
            Assert.That((view.PriorityUser, view.TokenHolder), Is.EqualTo(((UserId?)new UserId(9), (UserId?)new UserId(9))));
        }

        [Test]
        public void DeclaracaoTemAtacantesSemBloqueio()
        {
            CombatView combat = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-declaration.json").View.Combat!;

            Assert.That(combat.Attackers, Is.EqualTo(new[] { new CardInstanceId(21), new CardInstanceId(22) }));
            Assert.That(combat.Blocks, Is.Empty);
        }

        [Test]
        public void CombateTemParDeBloqueio()
        {
            BlockPair pair = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-combat-blocked.json").View.Combat!.Blocks.Single();

            Assert.That((pair.Blocker, pair.Attacker), Is.EqualTo((new CardInstanceId(40), new CardInstanceId(21))));
        }

        [Test]
        public void TerminadaTemDesfecho()
        {
            MatchOutcome outcome = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-finished.json").View.Outcome!;

            Assert.That((outcome.DefeatedUser, outcome.Reason, outcome.ReasonText), Is.EqualTo((new UserId(9), MatchEndReason.NexusDepleted, "nexus_depleted")));
        }

        [Test]
        public void MotivoDesconhecidoPreservaOTexto()
        {
            string json = MatchFixtures.Text("contract-match-update-finished.json")
                .Replace("\"outcome\": {\"defeated_user_id\": 9, \"reason\": \"nexus_depleted\"}", "\"outcome\": {\"defeated_user_id\": 9, \"reason\": \"timeout\"}");

            MatchOutcome outcome = MatchJson.Frame<MatchUpdateFrame>(json).View.Outcome!;

            Assert.That((outcome.Reason, outcome.ReasonText), Is.EqualTo((MatchEndReason.Unknown, "timeout")));
        }

        [Test]
        public void CampoAMaisEhIgnorado()
        {
            Assert.That(MatchJson.Decode(MatchFixtures.Text("contract-match-update-action.json")).IsValid, Is.True);
        }

        [Test]
        public void NexusFaltandoInvalidaOFrameComCaminho()
        {
            string text = MatchFixtures.Text("contract-match-start-mulligan.json");
            int ownNexus = text.IndexOf("\"nexus\": 20,", System.StringComparison.Ordinal);
            string json = text.Remove(ownNexus, "\"nexus\": 20,".Length);

            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode(json);

            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Failure.Path, Does.EndWith("view.you.nexus"));
        }
    }
}
