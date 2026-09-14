#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>
    /// Código de <c>message_refused</c> do socket de partida, conjunto fechado
    /// (<c>backend/specs/009-match-protocol/contracts/refusal_codes.md</c>). A recusa é comparada por este
    /// código, nunca pelo texto de <c>error</c>. Código novo do backend vira <see cref="Unknown"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// if (refusal.Code == PlayRefusalCode.NotEnoughEnergy) ShakeEnergyBar();
    /// </code>
    /// </example>
    public enum PlayRefusalCode
    {
        /// <summary><c>malformed_message</c>.</summary>
        MalformedMessage,

        /// <summary><c>unknown_message_type</c>.</summary>
        UnknownMessageType,

        /// <summary><c>match_not_found</c>.</summary>
        MatchNotFound,

        /// <summary><c>concurrent_match_write</c>.</summary>
        ConcurrentMatchWrite,

        /// <summary><c>internal_error</c>.</summary>
        InternalError,

        /// <summary><c>not_your_priority</c>.</summary>
        NotYourPriority,

        /// <summary><c>phase_forbids_action</c>.</summary>
        PhaseForbidsAction,

        /// <summary><c>match_is_over</c>.</summary>
        MatchIsOver,

        /// <summary><c>card_not_in_hand</c>.</summary>
        CardNotInHand,

        /// <summary><c>not_enough_energy</c>.</summary>
        NotEnoughEnergy,

        /// <summary><c>card_is_not_a_unit</c>.</summary>
        CardIsNotAUnit,

        /// <summary><c>bank_is_full</c>.</summary>
        BankIsFull,

        /// <summary><c>card_is_not_a_spell</c>.</summary>
        CardIsNotASpell,

        /// <summary><c>spell_takes_no_target</c>.</summary>
        SpellTakesNoTarget,

        /// <summary><c>spell_needs_target</c>.</summary>
        SpellNeedsTarget,

        /// <summary><c>wrong_spell_target_side</c>.</summary>
        WrongSpellTargetSide,

        /// <summary><c>spell_target_not_on_battlefield</c>.</summary>
        SpellTargetNotOnBattlefield,

        /// <summary><c>spell_only_in_declaration</c>.</summary>
        SpellOnlyInDeclaration,

        /// <summary><c>not_the_token_holder</c>.</summary>
        NotTheTokenHolder,

        /// <summary><c>attack_token_already_consumed</c>.</summary>
        AttackTokenAlreadyConsumed,

        /// <summary><c>bank_has_no_units</c>.</summary>
        BankHasNoUnits,

        /// <summary><c>no_attackers_selected</c>.</summary>
        NoAttackersSelected,

        /// <summary><c>attacker_not_in_bank</c>.</summary>
        AttackerNotInBank,

        /// <summary><c>duplicate_attacker</c>.</summary>
        DuplicateAttacker,

        /// <summary><c>unit_already_attacking</c>.</summary>
        UnitAlreadyAttacking,

        /// <summary><c>unit_is_not_attacking</c>.</summary>
        UnitIsNotAttacking,

        /// <summary><c>blocker_not_in_bank</c>.</summary>
        BlockerNotInBank,

        /// <summary><c>blocker_already_blocking</c>.</summary>
        BlockerAlreadyBlocking,

        /// <summary><c>attacker_already_blocked</c>.</summary>
        AttackerAlreadyBlocked,

        /// <summary><c>blocker_not_assigned</c>.</summary>
        BlockerNotAssigned,

        /// <summary><c>mulligan_already_taken</c>.</summary>
        MulliganAlreadyTaken,

        /// <summary>Código fora da tabela; o texto fica em <see cref="PlayRefusal.CodeText"/>.</summary>
        Unknown,
    }
}
