#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>attackers_sent</c>: unidades foram mandadas para a zona de ataque.</summary>
    /// <example><code>if (matchEvent is AttackersSentEvent sent) MoveToAttackZone(sent.Attackers);</code></example>
    public sealed class AttackersSentEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(AttackersSentEvent.KindName, AttackersSentEvent.Read);</code></example>
        public const string KindName = "attackers_sent";

        private AttackersSentEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Attackers = item.ReadCardInstanceIdList("attacker_card_instance_ids");
        }

        /// <summary>Quem mandou.</summary>
        /// <example><code>UserId user = sent.User;</code></example>
        public UserId User { get; }

        /// <summary>As unidades mandadas, na ordem.</summary>
        /// <example><code>int count = sent.Attackers.Count;</code></example>
        public IReadOnlyList<CardInstanceId> Attackers { get; }

        /// <summary><c>user_id</c>, <c>attacker_card_instance_ids</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = sent.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfInstances("attacker_card_instance_ids", Attackers) };

        internal static AttackersSentEvent Read(IPayloadReader item) => new AttackersSentEvent(item);
    }
}
