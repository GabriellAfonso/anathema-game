#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US1-7, FR-007, SC-002: os 31 códigos, o desconhecido, e <c>error</c> fora da decisão.</summary>
    public class PlayRefusalReaderTests
    {
        [TestCase("malformed_message", PlayRefusalCode.MalformedMessage)]
        [TestCase("unknown_message_type", PlayRefusalCode.UnknownMessageType)]
        [TestCase("match_not_found", PlayRefusalCode.MatchNotFound)]
        [TestCase("concurrent_match_write", PlayRefusalCode.ConcurrentMatchWrite)]
        [TestCase("internal_error", PlayRefusalCode.InternalError)]
        [TestCase("not_your_priority", PlayRefusalCode.NotYourPriority)]
        [TestCase("phase_forbids_action", PlayRefusalCode.PhaseForbidsAction)]
        [TestCase("match_is_over", PlayRefusalCode.MatchIsOver)]
        [TestCase("card_not_in_hand", PlayRefusalCode.CardNotInHand)]
        [TestCase("not_enough_energy", PlayRefusalCode.NotEnoughEnergy)]
        [TestCase("card_is_not_a_unit", PlayRefusalCode.CardIsNotAUnit)]
        [TestCase("bank_is_full", PlayRefusalCode.BankIsFull)]
        [TestCase("card_is_not_a_spell", PlayRefusalCode.CardIsNotASpell)]
        [TestCase("spell_takes_no_target", PlayRefusalCode.SpellTakesNoTarget)]
        [TestCase("spell_needs_target", PlayRefusalCode.SpellNeedsTarget)]
        [TestCase("wrong_spell_target_side", PlayRefusalCode.WrongSpellTargetSide)]
        [TestCase("spell_target_not_on_battlefield", PlayRefusalCode.SpellTargetNotOnBattlefield)]
        [TestCase("spell_only_in_declaration", PlayRefusalCode.SpellOnlyInDeclaration)]
        [TestCase("not_the_token_holder", PlayRefusalCode.NotTheTokenHolder)]
        [TestCase("attack_token_already_consumed", PlayRefusalCode.AttackTokenAlreadyConsumed)]
        [TestCase("bank_has_no_units", PlayRefusalCode.BankHasNoUnits)]
        [TestCase("no_attackers_selected", PlayRefusalCode.NoAttackersSelected)]
        [TestCase("attacker_not_in_bank", PlayRefusalCode.AttackerNotInBank)]
        [TestCase("duplicate_attacker", PlayRefusalCode.DuplicateAttacker)]
        [TestCase("unit_already_attacking", PlayRefusalCode.UnitAlreadyAttacking)]
        [TestCase("unit_is_not_attacking", PlayRefusalCode.UnitIsNotAttacking)]
        [TestCase("blocker_not_in_bank", PlayRefusalCode.BlockerNotInBank)]
        [TestCase("blocker_already_blocking", PlayRefusalCode.BlockerAlreadyBlocking)]
        [TestCase("attacker_already_blocked", PlayRefusalCode.AttackerAlreadyBlocked)]
        [TestCase("blocker_not_assigned", PlayRefusalCode.BlockerNotAssigned)]
        [TestCase("mulligan_already_taken", PlayRefusalCode.MulliganAlreadyTaken)]
        public void CodigoDoContratoViraOValor(string code, PlayRefusalCode expected)
        {
            PlayRefusal refusal = PlayRefusalReader.Read(Refused(code, "texto"), null);

            Assert.That((refusal.Code, refusal.CodeText), Is.EqualTo((expected, code)));
        }

        [Test]
        public void CodigoForaDaTabelaEhDesconhecidoComTexto()
        {
            PlayRefusal refusal = PlayRefusalReader.Read(Refused("brand_new_code", "novo"), null);

            Assert.That((refusal.Code, refusal.CodeText), Is.EqualTo((PlayRefusalCode.Unknown, "brand_new_code")));
        }

        [Test]
        public void ErrorEhPreservadoMasNaoDecide()
        {
            PlayRefusal first = PlayRefusalReader.Read(Refused("not_enough_energy", "costs 5 energy, has 1"), null);
            PlayRefusal second = PlayRefusalReader.Read(Refused("not_enough_energy", "custa 5"), null);

            Assert.That(first.Code, Is.EqualTo(second.Code));
            Assert.That((first.Error, second.Error), Is.EqualTo(("costs 5 energy, has 1", "custa 5")));
        }

        [Test]
        public void ComandoProvavelEhRepassado()
        {
            PlayCommand command = new PlayUnitCommand(new CardInstanceId(12));

            Assert.That(PlayRefusalReader.Read(Refused("bank_is_full", "x"), command).ProbableCommand, Is.SameAs(command));
        }

        private static MessageRefusedFrame Refused(string code, string error)
        {
            return MatchJson.Frame<MessageRefusedFrame>("{\"type\": \"message_refused\", \"payload\": {\"code\": \"" + code + "\", \"error\": \"" + error + "\"}}");
        }
    }
}
