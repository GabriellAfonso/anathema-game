#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>defense_ended</c>: o defensor apertou Resolver; o dano vem nos eventos seguintes.</summary>
    /// <example><code>if (matchEvent is DefenseEndedEvent) CloseDefenseWindow();</code></example>
    public sealed class DefenseEndedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(DefenseEndedEvent.KindName, DefenseEndedEvent.Read);</code></example>
        public const string KindName = "defense_ended";

        private DefenseEndedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
        }

        /// <summary>Quem encerrou a defesa.</summary>
        /// <example><code>UserId user = ended.User;</code></example>
        public UserId User { get; }

        /// <summary><c>user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = ended.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User) };

        internal static DefenseEndedEvent Read(IPayloadReader item) => new DefenseEndedEvent(item);
    }
}
