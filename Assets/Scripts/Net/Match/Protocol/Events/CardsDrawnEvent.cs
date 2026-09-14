#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>cards_drawn</c>: um jogador comprou. As cartas só vêm para o próprio jogador; para o oponente,
    /// <see cref="Cards"/> vem vazia e só <see cref="Count"/> diz quantas.
    /// </summary>
    /// <example><code>if (matchEvent is CardsDrawnEvent drawn) AnimateDraw(drawn.User, drawn.Count);</code></example>
    public sealed class CardsDrawnEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(CardsDrawnEvent.KindName, CardsDrawnEvent.Read);</code></example>
        public const string KindName = "cards_drawn";

        private CardsDrawnEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Count = item.ReadInteger("count");
            Cards = MatchCard.ReadList(item, "cards");
        }

        /// <summary>Quem comprou.</summary>
        /// <example><code>UserId user = drawn.User;</code></example>
        public UserId User { get; }

        /// <summary>Quantas cartas.</summary>
        /// <example><code>long count = drawn.Count;</code></example>
        public long Count { get; }

        /// <summary>As cartas compradas; vazia para o oponente.</summary>
        /// <example><code>bool mine = drawn.Cards.Count > 0;</code></example>
        public IReadOnlyList<MatchCard> Cards { get; }

        /// <summary><c>user_id</c>, <c>count</c>, <c>cards</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = drawn.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfUser("user_id", User), EventDetail.OfNumber("count", Count), EventDetail.OfCards("cards", Cards) };

        internal static CardsDrawnEvent Read(IPayloadReader item) => new CardsDrawnEvent(item);
    }
}
