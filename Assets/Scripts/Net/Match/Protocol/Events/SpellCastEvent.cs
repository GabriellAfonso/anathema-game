#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>spell_cast</c>: um feitiço resolveu, com alvo ou sem (<c>target_card_instance_id</c> nulo).</summary>
    /// <example><code>if (matchEvent is SpellCastEvent cast) AnimateSpell(cast.Card, cast.Target);</code></example>
    public sealed class SpellCastEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(SpellCastEvent.KindName, SpellCastEvent.Read);</code></example>
        public const string KindName = "spell_cast";

        private const string NoTargetText = "none";

        private SpellCastEvent(IPayloadReader item)
            : base(KindName)
        {
            User = item.ReadUserId("user_id");
            Card = MatchCard.Read(item.ReadObject("card"));
            Target = item.ReadOptionalCardInstanceId("target_card_instance_id");
        }

        /// <summary>Quem lançou.</summary>
        /// <example><code>UserId user = cast.User;</code></example>
        public UserId User { get; }

        /// <summary>O feitiço.</summary>
        /// <example><code>MatchCard card = cast.Card;</code></example>
        public MatchCard Card { get; }

        /// <summary>O alvo; nulo em feitiço sem alvo.</summary>
        /// <example><code>CardInstanceId? target = cast.Target;</code></example>
        public CardInstanceId? Target { get; }

        /// <summary><c>user_id</c>, <c>card</c>, <c>target_card_instance_id</c> (texto <c>none</c> sem alvo).</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = cast.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[]
        {
            EventDetail.OfUser("user_id", User),
            EventDetail.OfCard("card", Card),
            Target.HasValue ? EventDetail.OfInstance("target_card_instance_id", Target.Value) : EventDetail.OfText("target_card_instance_id", NoTargetText),
        };

        internal static SpellCastEvent Read(IPayloadReader item) => new SpellCastEvent(item);
    }
}
