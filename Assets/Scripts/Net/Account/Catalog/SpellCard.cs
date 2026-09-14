#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Carta de feitiço do catálogo. <see cref="Description"/> é para o jogador ler;
    /// <see cref="Effect"/> é para o cliente decidir a mira. Decidir lendo a descrição é bug.
    /// </summary>
    /// <example>
    /// <code>
    /// descriptionText.text = spell.Description;
    /// </code>
    /// </example>
    public sealed class SpellCard : CatalogCard
    {
        private SpellCard(IPayloadReader item)
            : base(item)
        {
            Description = item.ReadText("description");
            Effect = SpellEffect.Read(item.ReadObject("effect"));
        }

        /// <summary>Texto em português para o jogador.</summary>
        /// <example><code>string text = spell.Description;</code></example>
        public string Description { get; }

        /// <summary>Forma do efeito.</summary>
        /// <example><code>bool aim = spell.Effect.RequiresTarget;</code></example>
        public SpellEffect Effect { get; }

        internal static SpellCard Read(IPayloadReader item) => new SpellCard(item);
    }
}
