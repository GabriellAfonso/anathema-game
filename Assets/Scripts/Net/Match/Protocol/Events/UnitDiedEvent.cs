#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>unit_died</c>: uma unidade foi para o cemitério do dono.</summary>
    /// <example><code>if (matchEvent is UnitDiedEvent died) PlayDeath(died.Card);</code></example>
    public sealed class UnitDiedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(UnitDiedEvent.KindName, UnitDiedEvent.Read);</code></example>
        public const string KindName = "unit_died";

        private UnitDiedEvent(IPayloadReader item)
            : base(KindName)
        {
            Owner = item.ReadUserId("user_id");
            Card = MatchCard.Read(item.ReadObject("card"));
        }

        /// <summary>O dono da unidade.</summary>
        /// <example><code>UserId owner = died.Owner;</code></example>
        public UserId Owner { get; }

        /// <summary>A unidade morta.</summary>
        /// <example><code>MatchCard card = died.Card;</code></example>
        public MatchCard Card { get; }

        /// <summary><c>user_id</c>, <c>card</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = died.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", Owner), EventDetail.OfCard("card", Card) };

        internal static UnitDiedEvent Read(IPayloadReader item) => new UnitDiedEvent(item);
    }
}
