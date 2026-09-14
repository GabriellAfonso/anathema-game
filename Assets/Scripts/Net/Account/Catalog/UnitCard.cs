#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>Carta de unidade do catálogo, com ataque e vida.</summary>
    /// <example>
    /// <code>
    /// statsText.text = $"{unit.Attack}/{unit.Health}";
    /// </code>
    /// </example>
    public sealed class UnitCard : CatalogCard
    {
        private UnitCard(IPayloadReader item)
            : base(item)
        {
            Attack = item.ReadInteger("attack");
            Health = item.ReadInteger("health");
        }

        /// <summary>Ataque impresso na carta.</summary>
        /// <example><code>long attack = unit.Attack;</code></example>
        public long Attack { get; }

        /// <summary>Vida impressa na carta.</summary>
        /// <example><code>long health = unit.Health;</code></example>
        public long Health { get; }

        internal static UnitCard Read(IPayloadReader item) => new UnitCard(item);
    }
}
