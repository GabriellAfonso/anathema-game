#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>passed</c>: um jogador passou a vez.</summary>
    /// <example><code>if (matchEvent is PassedEvent passed) ShowPass(passed.User);</code></example>
    public sealed class PassedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(PassedEvent.KindName, PassedEvent.Read);</code></example>
        public const string KindName = "passed";

        private PassedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
        }

        /// <summary>Quem passou.</summary>
        /// <example><code>UserId user = passed.User;</code></example>
        public UserId User { get; }

        /// <summary><c>user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = passed.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User) };

        internal static PassedEvent Read(IPayloadReader item) => new PassedEvent(item);
    }
}
