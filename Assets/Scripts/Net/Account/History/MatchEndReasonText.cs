#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê o motivo do fim de partida (<c>nexus_depleted</c>, <c>forfeit</c>), o mesmo texto no histórico
    /// (feature 002) e no desfecho do socket de partida (feature 004). Um lugar só para o mapeamento:
    /// valor novo do backend vira <see cref="MatchEndReason.Unknown"/> nos dois.
    /// </summary>
    /// <example>
    /// <code>
    /// MatchEndReason reason = MatchEndReasonText.Parse(payload.ReadText("reason"));
    /// </code>
    /// </example>
    public static class MatchEndReasonText
    {
        private static readonly Dictionary<string, MatchEndReason> Reasons = new Dictionary<string, MatchEndReason>(StringComparer.Ordinal)
        {
            ["nexus_depleted"] = MatchEndReason.NexusDepleted,
            ["forfeit"] = MatchEndReason.Forfeit,
        };

        /// <summary>O motivo do texto recebido; texto fora do conjunto vira <see cref="MatchEndReason.Unknown"/>.</summary>
        /// <example><code>MatchEndReason reason = MatchEndReasonText.Parse("forfeit"); // Forfeit</code></example>
        public static MatchEndReason Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), "end reason text is null: expected the reason text as sent, like nexus_depleted");

            return Reasons.TryGetValue(text, out MatchEndReason reason) ? reason : MatchEndReason.Unknown;
        }
    }
}
