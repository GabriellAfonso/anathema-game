#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê a duração de um efeito (<c>permanent</c>, <c>until_end_of_round</c>), o mesmo texto no efeito do
    /// catálogo (feature 002) e nos modificadores de unidade da partida (feature 004). Um lugar só para o
    /// mapeamento: valor novo do backend vira <see cref="SpellDuration.Unknown"/> nos dois.
    /// </summary>
    /// <example>
    /// <code>
    /// SpellDuration duration = SpellDurationText.Parse(modifier.ReadText("duration"));
    /// </code>
    /// </example>
    internal static class SpellDurationText
    {
        private static readonly Dictionary<string, SpellDuration> Durations = new Dictionary<string, SpellDuration>(StringComparer.Ordinal)
        {
            ["permanent"] = SpellDuration.Permanent,
            ["until_end_of_round"] = SpellDuration.UntilEndOfRound,
        };

        /// <summary>A duração do texto recebido; texto fora do conjunto vira <see cref="SpellDuration.Unknown"/>.</summary>
        /// <example><code>SpellDuration duration = SpellDurationText.Parse("permanent"); // Permanent</code></example>
        public static SpellDuration Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), "duration text is null: expected the duration text as sent, like permanent");

            return Durations.TryGetValue(text, out SpellDuration duration) ? duration : SpellDuration.Unknown;
        }
    }
}
