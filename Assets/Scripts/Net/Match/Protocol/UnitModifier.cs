#nullable enable
using Anathema.Net.Account;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Um modificador de unidade no banco, união por <c>modifier_kind</c>
    /// (<c>backend/server/apps/game/match/documents.py</c>, <c>ModifierDocument</c>). A duração usa a mesma
    /// leitura do efeito do catálogo (<see cref="SpellDurationText"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// if (modifier is AttackModifier attack) bonus += attack.Amount;
    /// </code>
    /// </example>
    public abstract class UnitModifier
    {
        private protected UnitModifier(string kindText, string durationText)
        {
            KindText = kindText;
            DurationText = durationText;
            Duration = SpellDurationText.Parse(durationText);
        }

        /// <summary>O texto de <c>modifier_kind</c> como veio.</summary>
        /// <example><code>string kind = modifier.KindText; // "attack"</code></example>
        public string KindText { get; }

        /// <summary>Por quanto tempo vale.</summary>
        /// <example><code>bool temporary = modifier.Duration == SpellDuration.UntilEndOfRound;</code></example>
        public SpellDuration Duration { get; }

        /// <summary>O texto de <c>duration</c> como veio; vazio quando um modificador desconhecido não o traz.</summary>
        /// <example><code>string raw = modifier.DurationText;</code></example>
        public string DurationText { get; }
    }
}
