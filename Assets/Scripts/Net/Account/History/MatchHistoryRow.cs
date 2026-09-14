#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Uma partida do histórico, do ponto de vista de quem pede
    /// (<c>backend/specs/012-match-result-history/contracts/http_match_history.md</c>).
    /// <see cref="EndedAtText"/> é só para mostrar: nunca serve para medir tempo.
    /// </summary>
    /// <example>
    /// <code>
    /// resultText.text = row.Won ? "Vitória" : "Derrota";
    /// </code>
    /// </example>
    public sealed class MatchHistoryRow
    {
        private MatchHistoryRow(IPayloadReader row)
        {
            Match = row.ReadMatchId("match_id");
            Won = row.ReadBoolean("won");
            EndReasonText = row.ReadText("end_reason");
            // O mesmo texto chega no desfecho do socket de partida; um mapeamento só (specs/004-match-session/research.md, R2).
            EndReason = MatchEndReasonText.Parse(EndReasonText);
            IPayloadReader? opponent = row.ReadOptionalObject("opponent");
            Opponent = opponent == null ? null : HistoryOpponent.Read(opponent);
            DurationSeconds = row.ReadInteger("duration_seconds");
            FinalRound = row.ReadInteger("final_round");
            EndedAtText = row.ReadText("ended_at");
        }

        /// <summary>A mesma partida vista no socket.</summary>
        /// <example><code>MatchId match = row.Match;</code></example>
        public MatchId Match { get; }

        /// <summary>Quem pede venceu.</summary>
        /// <example><code>bool won = row.Won;</code></example>
        public bool Won { get; }

        /// <summary>Motivo do fim.</summary>
        /// <example><code>MatchEndReason reason = row.EndReason;</code></example>
        public MatchEndReason EndReason { get; }

        /// <summary>O texto de <c>end_reason</c> como veio.</summary>
        /// <example><code>string raw = row.EndReasonText;</code></example>
        public string EndReasonText { get; }

        /// <summary>O oponente, ou nulo quando o perfil dele foi apagado depois da partida.</summary>
        /// <example><code>string name = row.Opponent?.Nickname ?? "?";</code></example>
        public HistoryOpponent? Opponent { get; }

        /// <summary>Duração em segundos; 0 em partida anterior ao registro de duração.</summary>
        /// <example><code>long seconds = row.DurationSeconds;</code></example>
        public long DurationSeconds { get; }

        /// <summary>Rodada em que acabou.</summary>
        /// <example><code>long round = row.FinalRound;</code></example>
        public long FinalRound { get; }

        /// <summary>Instante do fim em ISO 8601 UTC, como texto, só para exibir.</summary>
        /// <example><code>dateText.text = row.EndedAtText;</code></example>
        public string EndedAtText { get; }

        internal static MatchHistoryRow Read(IPayloadReader row) => new MatchHistoryRow(row);
    }
}
