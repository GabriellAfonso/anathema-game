#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Quanto dura o efeito de um feitiço, pelo conjunto fechado de <c>duration</c> do contrato de catálogo.</summary>
    /// <example><code>bool temporary = spell.Effect.Duration == SpellDuration.UntilEndOfRound;</code></example>
    public enum SpellDuration
    {
        /// <summary><c>permanent</c>.</summary>
        Permanent,

        /// <summary><c>until_end_of_round</c>.</summary>
        UntilEndOfRound,

        /// <summary>Valor fora do conjunto; o texto fica em <c>DurationText</c>.</summary>
        Unknown,
    }
}
