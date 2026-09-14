#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>nexus_changed</c>: o Nexus de um jogador mudou. O Nexus a desenhar está na visão.</summary>
    /// <example><code>if (matchEvent is NexusChangedEvent changed) PulseNexus(changed.User);</code></example>
    public sealed class NexusChangedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(NexusChangedEvent.KindName, NexusChangedEvent.Read);</code></example>
        public const string KindName = "nexus_changed";

        private NexusChangedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Amount = item.ReadInteger("amount");
        }

        /// <summary>De quem.</summary>
        /// <example><code>UserId user = changed.User;</code></example>
        public UserId User { get; }

        /// <summary>O <c>amount</c> como veio.</summary>
        /// <example><code>long amount = changed.Amount;</code></example>
        public long Amount { get; }

        /// <summary><c>user_id</c>, <c>amount</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = changed.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfNumber("amount", Amount) };

        internal static NexusChangedEvent Read(IPayloadReader item) => new NexusChangedEvent(item);
    }
}
