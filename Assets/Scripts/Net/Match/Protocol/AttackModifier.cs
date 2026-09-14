#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>modifier_kind</c> <c>attack</c>: soma <see cref="Amount"/> ao ataque exibido.</summary>
    /// <example>
    /// <code>
    /// long bonus = unit.Modifiers.OfType&lt;AttackModifier&gt;().Sum(modifier => modifier.Amount);
    /// </code>
    /// </example>
    public sealed class AttackModifier : UnitModifier
    {
        /// <summary>Valor de <c>modifier_kind</c>.</summary>
        /// <example><code>union.Register(AttackModifier.KindName, AttackModifier.Read);</code></example>
        public const string KindName = "attack";

        private AttackModifier(IPayloadReader modifier)
            : base(KindName, modifier.ReadText("duration"))
        {
            Amount = modifier.ReadInteger("amount");
        }

        /// <summary>Quanto soma ao ataque.</summary>
        /// <example><code>long amount = modifier.Amount;</code></example>
        public long Amount { get; }

        internal static AttackModifier Read(IPayloadReader modifier) => new AttackModifier(modifier);
    }
}
