#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Quem perdeu e por quê (<c>documents.py</c>, <c>MatchOutcomeDocument</c>). O mesmo formato chega em
    /// <c>view.outcome</c> e no evento <c>match_finished</c>; o motivo usa a leitura do histórico
    /// (<see cref="MatchEndReasonText"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// bool won = outcome.DefeatedUser != mirror.Self;
    /// </code>
    /// </example>
    public sealed class MatchOutcome
    {
        private MatchOutcome(IPayloadReader outcome)
        {
            DefeatedUser = outcome.ReadUserId("defeated_user_id");
            ReasonText = outcome.ReadText("reason");
            Reason = MatchEndReasonText.Parse(ReasonText);
        }

        /// <summary>O jogador derrotado.</summary>
        /// <example><code>UserId loser = outcome.DefeatedUser;</code></example>
        public UserId DefeatedUser { get; }

        /// <summary>O motivo.</summary>
        /// <example><code>bool forfeit = outcome.Reason == MatchEndReason.Forfeit;</code></example>
        public MatchEndReason Reason { get; }

        /// <summary>O texto de <c>reason</c> como veio.</summary>
        /// <example><code>string raw = outcome.ReasonText;</code></example>
        public string ReasonText { get; }

        /// <summary>Lê um objeto com <c>defeated_user_id</c> e <c>reason</c>.</summary>
        /// <example><code>MatchOutcome outcome = MatchOutcome.Read(view.ReadObject("outcome"));</code></example>
        public static MatchOutcome Read(IPayloadReader outcome) => new MatchOutcome(outcome);
    }
}
