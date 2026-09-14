#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>turn_timed_out</c>: o servidor estourou a vez; o evento seguinte é a ação automática
    /// (<c>backend/specs/010-match-timers/contracts/server_frames.md</c>). O cliente nunca estoura nada.
    /// </summary>
    /// <example><code>if (matchEvent is TurnTimedOutEvent timedOut) ShowTimeout(timedOut.User);</code></example>
    public sealed class TurnTimedOutEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(TurnTimedOutEvent.KindName, TurnTimedOutEvent.Read);</code></example>
        public const string KindName = "turn_timed_out";

        private TurnTimedOutEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            TurnNumber = item.ReadInteger("turn_number");
        }

        /// <summary>De quem era a vez.</summary>
        /// <example><code>UserId user = timedOut.User;</code></example>
        public UserId User { get; }

        /// <summary>A vez que estourou.</summary>
        /// <example><code>long turn = timedOut.TurnNumber;</code></example>
        public long TurnNumber { get; }

        /// <summary><c>user_id</c>, <c>turn_number</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = timedOut.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfNumber("turn_number", TurnNumber) };

        internal static TurnTimedOutEvent Read(IPayloadReader item) => new TurnTimedOutEvent(item);
    }
}
