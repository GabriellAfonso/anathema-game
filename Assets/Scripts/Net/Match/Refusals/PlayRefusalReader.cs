#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Lê <c>message_refused</c> do socket de partida pelo <c>code</c>, numa tabela fechada
    /// (<c>backend/specs/009-match-protocol/contracts/refusal_codes.md</c>). <c>error</c> nunca decide nada.
    /// </summary>
    internal static class PlayRefusalReader
    {
        private static readonly Dictionary<string, PlayRefusalCode> Codes = new Dictionary<string, PlayRefusalCode>(StringComparer.Ordinal)
        {
            ["malformed_message"] = PlayRefusalCode.MalformedMessage,
            ["unknown_message_type"] = PlayRefusalCode.UnknownMessageType,
            ["match_not_found"] = PlayRefusalCode.MatchNotFound,
            ["concurrent_match_write"] = PlayRefusalCode.ConcurrentMatchWrite,
            ["internal_error"] = PlayRefusalCode.InternalError,
            ["not_your_priority"] = PlayRefusalCode.NotYourPriority,
            ["phase_forbids_action"] = PlayRefusalCode.PhaseForbidsAction,
            ["match_is_over"] = PlayRefusalCode.MatchIsOver,
            ["card_not_in_hand"] = PlayRefusalCode.CardNotInHand,
            ["not_enough_energy"] = PlayRefusalCode.NotEnoughEnergy,
            ["card_is_not_a_unit"] = PlayRefusalCode.CardIsNotAUnit,
            ["bank_is_full"] = PlayRefusalCode.BankIsFull,
            ["card_is_not_a_spell"] = PlayRefusalCode.CardIsNotASpell,
            ["spell_takes_no_target"] = PlayRefusalCode.SpellTakesNoTarget,
            ["spell_needs_target"] = PlayRefusalCode.SpellNeedsTarget,
            ["wrong_spell_target_side"] = PlayRefusalCode.WrongSpellTargetSide,
            ["spell_target_not_on_battlefield"] = PlayRefusalCode.SpellTargetNotOnBattlefield,
            ["spell_only_in_declaration"] = PlayRefusalCode.SpellOnlyInDeclaration,
            ["not_the_token_holder"] = PlayRefusalCode.NotTheTokenHolder,
            ["attack_token_already_consumed"] = PlayRefusalCode.AttackTokenAlreadyConsumed,
            ["bank_has_no_units"] = PlayRefusalCode.BankHasNoUnits,
            ["no_attackers_selected"] = PlayRefusalCode.NoAttackersSelected,
            ["attacker_not_in_bank"] = PlayRefusalCode.AttackerNotInBank,
            ["duplicate_attacker"] = PlayRefusalCode.DuplicateAttacker,
            ["unit_already_attacking"] = PlayRefusalCode.UnitAlreadyAttacking,
            ["unit_is_not_attacking"] = PlayRefusalCode.UnitIsNotAttacking,
            ["blocker_not_in_bank"] = PlayRefusalCode.BlockerNotInBank,
            ["blocker_already_blocking"] = PlayRefusalCode.BlockerAlreadyBlocking,
            ["attacker_already_blocked"] = PlayRefusalCode.AttackerAlreadyBlocked,
            ["blocker_not_assigned"] = PlayRefusalCode.BlockerNotAssigned,
            ["mulligan_already_taken"] = PlayRefusalCode.MulliganAlreadyTaken,
        };

        internal static PlayRefusal Read(MessageRefusedFrame refusal, PlayCommand? probableCommand)
        {
            PlayRefusalCode code = Codes.TryGetValue(refusal.Code, out PlayRefusalCode known) ? known : PlayRefusalCode.Unknown;
            return new PlayRefusal(code, refusal.Code, refusal.Error, probableCommand);
        }
    }
}
