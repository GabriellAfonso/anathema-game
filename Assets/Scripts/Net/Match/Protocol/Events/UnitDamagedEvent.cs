#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>unit_damaged</c>: uma unidade recebeu dano. O dano acumulado a desenhar está na visão.</summary>
    /// <example><code>if (matchEvent is UnitDamagedEvent damaged) FloatDamage(damaged.Unit, damaged.Amount);</code></example>
    public sealed class UnitDamagedEvent : MatchEvent
    {
        /// <summary>Valor de <c>kind</c>.</summary>
        /// <example><code>union.Register(UnitDamagedEvent.KindName, UnitDamagedEvent.Read);</code></example>
        public const string KindName = "unit_damaged";

        private UnitDamagedEvent(IPayloadReader item)
            : base(KindName)
        {
            Unit = item.ReadCardInstanceId("card_instance_id");
            Amount = item.ReadInteger("amount");
        }

        /// <summary>A unidade atingida.</summary>
        /// <example><code>CardInstanceId unit = damaged.Unit;</code></example>
        public CardInstanceId Unit { get; }

        /// <summary>Quanto dano.</summary>
        /// <example><code>long amount = damaged.Amount;</code></example>
        public long Amount { get; }

        /// <summary><c>card_instance_id</c>, <c>amount</c>.</summary>
        /// <example><code>IReadOnlyList&lt;EventDetail&gt; details = damaged.Details;</code></example>
        public override IReadOnlyList<EventDetail> Details => new[] { EventDetail.OfInstance("card_instance_id", Unit), EventDetail.OfNumber("amount", Amount) };

        internal static UnitDamagedEvent Read(IPayloadReader item) => new UnitDamagedEvent(item);
    }
}
