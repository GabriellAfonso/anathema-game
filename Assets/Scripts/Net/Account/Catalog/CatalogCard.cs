#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Uma carta do catálogo: os campos comuns de unidade e feitiço. Hierarquia fechada; os braços
    /// são <see cref="UnitCard"/> e <see cref="SpellCard"/> (constituição, princípio IV).
    /// </summary>
    /// <example>
    /// <code>
    /// nameText.text = card.Name;
    /// </code>
    /// </example>
    public abstract class CatalogCard
    {
        private protected CatalogCard(IPayloadReader item)
        {
            Card = item.ReadCardId("card_id");
            Name = item.ReadText("name");
            Energy = item.ReadInteger("energy");
            ImageKey = item.ReadText("image");
        }

        /// <summary>Identificador no catálogo.</summary>
        /// <example><code>CardId card = entry.Card;</code></example>
        public CardId Card { get; }

        /// <summary>Nome para o jogador ler.</summary>
        /// <example><code>string name = entry.Name;</code></example>
        public string Name { get; }

        /// <summary>Custo em energia.</summary>
        /// <example><code>long cost = entry.Energy;</code></example>
        public long Energy { get; }

        /// <summary>Chave da arte, resolvida pela apresentação.</summary>
        /// <example><code>Sprite art = Resources.Load&lt;Sprite&gt;(entry.ImageKey);</code></example>
        public string ImageKey { get; }
    }
}
