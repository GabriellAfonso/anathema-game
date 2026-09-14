#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>attack_confirmed</c>: o atacante apertou Atacar; a defesa começa.</summary>
    /// <example><code>if (matchEvent is AttackConfirmedEvent) ShowDefenseWindow();</code></example>
    public sealed class AttackConfirmedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(AttackConfirmedEvent.KindName, AttackConfirmedEvent.Read);</code></example>
        public const string KindName = "attack_confirmed";

        private AttackConfirmedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
        }

        /// <summary>Quem confirmou.</summary>
        /// <example><code>UserId user = confirmed.User;</code></example>
        public UserId User { get; }

        /// <summary><c>user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = confirmed.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User) };

        internal static AttackConfirmedEvent Read(IPayloadReader item) => new AttackConfirmedEvent(item);
    }
}
