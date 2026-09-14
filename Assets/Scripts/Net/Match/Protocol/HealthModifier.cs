#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>modifier_kind</c> <c>health</c>: soma <see cref="Amount"/> à vida exibida.</summary>
    /// <example>
    /// <code>
    /// long bonus = unit.Modifiers.OfType&lt;HealthModifier&gt;().Sum(modifier => modifier.Amount);
    /// </code>
    /// </example>
    public sealed class HealthModifier : UnitModifier
    {
        /// <summary>Valor de <c>modifier_kind</c>.</summary>
        /// <example><code>union.Register(HealthModifier.KindName, HealthModifier.Read);</code></example>
        public const string KindName = "health";

        private HealthModifier(IPayloadReader modifier)
            : base(KindName, modifier.ReadText("duration"))
        {
            Amount = modifier.ReadInteger("amount");
        }

        /// <summary>Quanto soma à vida.</summary>
        /// <example><code>long amount = modifier.Amount;</code></example>
        public long Amount { get; }

        internal static HealthModifier Read(IPayloadReader modifier) => new HealthModifier(modifier);
    }
}
