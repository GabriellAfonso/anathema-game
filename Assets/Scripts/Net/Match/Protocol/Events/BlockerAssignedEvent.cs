#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>blocker_assigned</c>: o defensor pôs uma unidade na frente de um atacante.</summary>
    /// <example><code>if (matchEvent is BlockerAssignedEvent assigned) DrawBlockLine(assigned.Blocker, assigned.Attacker);</code></example>
    public sealed class BlockerAssignedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(BlockerAssignedEvent.KindName, BlockerAssignedEvent.Read);</code></example>
        public const string KindName = "blocker_assigned";

        private BlockerAssignedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Blocker = item.ReadCardInstanceId("blocker_card_instance_id");
            Attacker = item.ReadCardInstanceId("attacker_card_instance_id");
        }

        /// <summary>Quem bloqueou.</summary>
        /// <example><code>UserId user = assigned.User;</code></example>
        public UserId User { get; }

        /// <summary>A unidade que bloqueia.</summary>
        /// <example><code>CardInstanceId blocker = assigned.Blocker;</code></example>
        public CardInstanceId Blocker { get; }

        /// <summary>O atacante bloqueado.</summary>
        /// <example><code>CardInstanceId attacker = assigned.Attacker;</code></example>
        public CardInstanceId Attacker { get; }

        /// <summary><c>user_id</c>, <c>blocker_card_instance_id</c>, <c>attacker_card_instance_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = assigned.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[]
        {
            EventDetail.OfUser("user_id", User),
            EventDetail.OfInstance("blocker_card_instance_id", Blocker),
            EventDetail.OfInstance("attacker_card_instance_id", Attacker),
        };

        internal static BlockerAssignedEvent Read(IPayloadReader item) => new BlockerAssignedEvent(item);
    }
}
