#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>mulligan_timed_out</c>: o servidor confirmou o mulligan de alguém sem trocar nada.</summary>
    /// <example><code>if (matchEvent is MulliganTimedOutEvent timedOut) ShowTimeout(timedOut.User);</code></example>
    public sealed class MulliganTimedOutEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(MulliganTimedOutEvent.KindName, MulliganTimedOutEvent.Read);</code></example>
        public const string KindName = "mulligan_timed_out";

        private MulliganTimedOutEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
        }

        /// <summary>De quem era o mulligan.</summary>
        /// <example><code>UserId user = timedOut.User;</code></example>
        public UserId User { get; }

        /// <summary><c>user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = timedOut.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User) };

        internal static MulliganTimedOutEvent Read(IPayloadReader item) => new MulliganTimedOutEvent(item);
    }
}
