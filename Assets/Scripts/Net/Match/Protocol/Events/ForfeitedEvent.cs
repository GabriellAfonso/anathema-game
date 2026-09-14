#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>forfeited</c>: um jogador desistiu; o desfecho vem em <c>match_finished</c> e na visão.</summary>
    /// <example><code>if (matchEvent is ForfeitedEvent forfeited) ShowForfeit(forfeited.User);</code></example>
    public sealed class ForfeitedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(ForfeitedEvent.KindName, ForfeitedEvent.Read);</code></example>
        public const string KindName = "forfeited";

        private ForfeitedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
        }

        /// <summary>Quem desistiu.</summary>
        /// <example><code>UserId user = forfeited.User;</code></example>
        public UserId User { get; }

        /// <summary><c>user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = forfeited.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User) };

        internal static ForfeitedEvent Read(IPayloadReader item) => new ForfeitedEvent(item);
    }
}
