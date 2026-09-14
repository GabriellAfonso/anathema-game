#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>blocker_removed</c>: o defensor tirou um bloqueador.</summary>
    /// <example><code>if (matchEvent is BlockerRemovedEvent removed) EraseBlockLine(removed.Blocker);</code></example>
    public sealed class BlockerRemovedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(BlockerRemovedEvent.KindName, BlockerRemovedEvent.Read);</code></example>
        public const string KindName = "blocker_removed";

        private BlockerRemovedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Blocker = item.ReadCardInstanceId("blocker_card_instance_id");
        }

        /// <summary>Quem tirou.</summary>
        /// <example><code>UserId user = removed.User;</code></example>
        public UserId User { get; }

        /// <summary>O bloqueador tirado.</summary>
        /// <example><code>CardInstanceId blocker = removed.Blocker;</code></example>
        public CardInstanceId Blocker { get; }

        /// <summary><c>user_id</c>, <c>blocker_card_instance_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = removed.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfInstance("blocker_card_instance_id", Blocker) };

        internal static BlockerRemovedEvent Read(IPayloadReader item) => new BlockerRemovedEvent(item);
    }
}
