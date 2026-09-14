#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>mulligan_taken</c>: um jogador respondeu o mulligan. Do oponente, só a contagem, nunca quais cartas.</summary>
    /// <example><code>if (matchEvent is MulliganTakenEvent taken) ShowAnswered(taken.User);</code></example>
    public sealed class MulliganTakenEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(MulliganTakenEvent.KindName, MulliganTakenEvent.Read);</code></example>
        public const string KindName = "mulligan_taken";

        private MulliganTakenEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            SwappedCount = item.ReadInteger("swapped_count");
        }

        /// <summary>Quem respondeu.</summary>
        /// <example><code>UserId user = taken.User;</code></example>
        public UserId User { get; }

        /// <summary>Quantas cartas trocou.</summary>
        /// <example><code>long swapped = taken.SwappedCount;</code></example>
        public long SwappedCount { get; }

        /// <summary><c>user_id</c>, <c>swapped_count</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = taken.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfNumber("swapped_count", SwappedCount) };

        internal static MulliganTakenEvent Read(IPayloadReader item) => new MulliganTakenEvent(item);
    }
}
