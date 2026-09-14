#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O catálogo carregado numa geração da sessão: as cartas na ordem do servidor e a consulta por
    /// <see cref="CardId"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// CardLookup lookup = loaded.Find(new CardId(1004));
    /// </code>
    /// </example>
    public sealed class LoadedCatalog
    {
        private readonly Dictionary<CardId, CatalogCard> byCard = new Dictionary<CardId, CatalogCard>();

        internal LoadedCatalog(IReadOnlyList<CatalogCard> cards, int generation)
        {
            Cards = cards;
            Generation = generation;
            foreach (CatalogCard card in cards)
                byCard[card.Card] = card;
        }

        /// <summary>Cartas legíveis, na ordem do servidor.</summary>
        /// <example><code>int total = loaded.Cards.Count;</code></example>
        public IReadOnlyList<CatalogCard> Cards { get; }

        /// <summary>Geração da sessão em que foi carregado.</summary>
        /// <example><code>bool stale = loaded.Generation != session.Generation;</code></example>
        public int Generation { get; }

        /// <summary>Procura a carta; ausente é <see cref="CardLookupKind.NotFound"/>.</summary>
        /// <example><code>CardLookup lookup = loaded.Find(card);</code></example>
        public CardLookup Find(CardId card)
        {
            return CardLookup.Of(card, byCard.TryGetValue(card, out CatalogCard? found) ? found : null);
        }
    }
}
