#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>match_finished</c>: a partida acabou, com o mesmo desfecho que a visão traz em <c>outcome</c>.</summary>
    /// <example><code>if (matchEvent is MatchFinishedEvent finished) ShowResult(finished.Outcome);</code></example>
    public sealed class MatchFinishedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(MatchFinishedEvent.KindName, MatchFinishedEvent.Read);</code></example>
        public const string KindName = "match_finished";

        private MatchFinishedEvent(IPayloadReader item)
            : base(KindName)
        {
            // O evento traz defeated_user_id e reason no próprio objeto, com os nomes do outcome da visão.
            Outcome = MatchOutcome.Read(item);
        }

        /// <summary>Quem perdeu e por quê.</summary>
        /// <example><code>UserId loser = finished.Outcome.DefeatedUser;</code></example>
        public MatchOutcome Outcome { get; }

        /// <summary><c>defeated_user_id</c>, <c>reason</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = finished.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("defeated_user_id", Outcome.DefeatedUser), EventDetail.OfText("reason", Outcome.ReasonText) };

        internal static MatchFinishedEvent Read(IPayloadReader item) => new MatchFinishedEvent(item);
    }
}
