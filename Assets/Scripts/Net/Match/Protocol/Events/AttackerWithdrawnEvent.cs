#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>attacker_withdrawn</c>: um atacante voltou da zona de ataque para o banco.</summary>
    /// <example><code>if (matchEvent is AttackerWithdrawnEvent withdrawn) MoveToBank(withdrawn.Attacker);</code></example>
    public sealed class AttackerWithdrawnEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(AttackerWithdrawnEvent.KindName, AttackerWithdrawnEvent.Read);</code></example>
        public const string KindName = "attacker_withdrawn";

        private AttackerWithdrawnEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Attacker = item.ReadCardInstanceId("attacker_card_instance_id");
        }

        /// <summary>Quem puxou de volta.</summary>
        /// <example><code>UserId user = withdrawn.User;</code></example>
        public UserId User { get; }

        /// <summary>O atacante puxado.</summary>
        /// <example><code>CardInstanceId attacker = withdrawn.Attacker;</code></example>
        public CardInstanceId Attacker { get; }

        /// <summary><c>user_id</c>, <c>attacker_card_instance_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = withdrawn.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfInstance("attacker_card_instance_id", Attacker) };

        internal static AttackerWithdrawnEvent Read(IPayloadReader item) => new AttackerWithdrawnEvent(item);
    }
}
