#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// O resultado de uma partida terminada: o desfecho do espelho na hora, e a linha do histórico quando chegar
    /// (specs/005-presentation-facade/contracts/client-state.md, "Linha do histórico").
    /// </summary>
    /// <example>
    /// <code>
    /// MatchResult result = client.State.Result!;
    /// title.text = result.Won ? "Vitória" : "Derrota";
    /// if (result.RowStatus == HistoryRowStatus.Resolved) rounds.text = result.Row!.FinalRound.ToString();
    /// </code>
    /// </example>
    public sealed class MatchResult
    {
        internal const int MaxAttempts = 4;

        private MatchResult(MatchId match, MatchOutcome? outcome, bool won, HistoryRowStatus rowStatus, MatchHistoryRow? row, int attempts)
        {
            Match = match;
            Outcome = outcome;
            Won = won;
            RowStatus = rowStatus;
            Row = row;
            Attempts = attempts;
        }

        /// <summary>A partida.</summary>
        /// <example><code>MatchId match = result.Match;</code></example>
        public MatchId Match { get; }

        /// <summary>O desfecho do espelho; nulo só se o frame final vier sem desfecho (fora do contrato).</summary>
        /// <example><code>MatchEndReason? reason = result.Outcome?.Reason;</code></example>
        public MatchOutcome? Outcome { get; }

        /// <summary>O próprio jogador venceu, pelo espelho.</summary>
        /// <example><code>bool won = result.Won;</code></example>
        public bool Won { get; }

        /// <summary>Onde está a linha do histórico.</summary>
        /// <example><code>bool waiting = result.RowStatus == HistoryRowStatus.Fetching;</code></example>
        public HistoryRowStatus RowStatus { get; }

        /// <summary>A linha do histórico; só em <see cref="HistoryRowStatus.Resolved"/>.</summary>
        /// <example><code>long rounds = result.Row?.FinalRound ?? 0;</code></example>
        public MatchHistoryRow? Row { get; }

        /// <summary>Leituras do histórico feitas, de 0 a 4.</summary>
        /// <example><code>int reads = result.Attempts;</code></example>
        public int Attempts { get; }

        internal static MatchResult Fetching(MatchId match, MatchOutcome? outcome, bool won) => new MatchResult(match, outcome, won, HistoryRowStatus.Fetching, null, 0);

        internal MatchResult Resolved(MatchHistoryRow row, int attempts)
        {
            MatchHistoryRow required = row ?? throw new ArgumentNullException(nameof(row), $"history row of {Match} is null: expected the row with the same match_id");
            return new MatchResult(Match, Outcome, Won, HistoryRowStatus.Resolved, required, RequireAttempts(attempts));
        }

        internal MatchResult Unavailable(int attempts) => new MatchResult(Match, Outcome, Won, HistoryRowStatus.Unavailable, null, RequireAttempts(attempts));

        private static int RequireAttempts(int attempts)
        {
            if (attempts < 1 || attempts > MaxAttempts)
                throw new ArgumentOutOfRangeException(nameof(attempts), attempts, $"history attempts is {attempts}: expected 1 to {MaxAttempts}");

            return attempts;
        }
    }
}
