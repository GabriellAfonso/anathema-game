#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>round_started</c>: uma rodada começou, inclusive a Rodada 1 depois do mulligan.</summary>
    /// <example><code>if (matchEvent is RoundStartedEvent started) ShowRoundBanner(started.RoundNumber);</code></example>
    public sealed class RoundStartedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(RoundStartedEvent.KindName, RoundStartedEvent.Read);</code></example>
        public const string KindName = "round_started";

        private RoundStartedEvent(IPayloadReader item)
            : base(KindName)
        {
            RoundNumber = item.ReadInteger("round_number");
            TokenHolder = item.ReadUserId("token_holder_user_id");
        }

        /// <summary>A rodada que começou.</summary>
        /// <example><code>long round = started.RoundNumber;</code></example>
        public long RoundNumber { get; }

        /// <summary>Quem tem o token nesta rodada.</summary>
        /// <example><code>UserId holder = started.TokenHolder;</code></example>
        public UserId TokenHolder { get; }

        /// <summary><c>round_number</c>, <c>token_holder_user_id</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = started.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfNumber("round_number", RoundNumber), EventDetail.OfUser("token_holder_user_id", TokenHolder) };

        internal static RoundStartedEvent Read(IPayloadReader item) => new RoundStartedEvent(item);
    }
}
