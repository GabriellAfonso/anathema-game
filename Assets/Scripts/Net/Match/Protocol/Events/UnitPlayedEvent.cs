#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>unit_played</c>: uma unidade saiu da mão para o banco.</summary>
    /// <example><code>if (matchEvent is UnitPlayedEvent played) AnimateSummon(played.Card);</code></example>
    public sealed class UnitPlayedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(UnitPlayedEvent.KindName, UnitPlayedEvent.Read);</code></example>
        public const string KindName = "unit_played";

        private UnitPlayedEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Card = MatchCard.Read(item.ReadObject("card"));
        }

        /// <summary>Quem jogou.</summary>
        /// <example><code>UserId user = played.User;</code></example>
        public UserId User { get; }

        /// <summary>A unidade jogada.</summary>
        /// <example><code>MatchCard card = played.Card;</code></example>
        public MatchCard Card { get; }

        /// <summary><c>user_id</c>, <c>card</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = played.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfCard("card", Card) };

        internal static UnitPlayedEvent Read(IPayloadReader item) => new UnitPlayedEvent(item);
    }
}
