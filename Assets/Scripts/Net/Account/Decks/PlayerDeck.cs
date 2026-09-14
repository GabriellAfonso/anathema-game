#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>Um deck do jogador autenticado: identidade, nome e a lista de cartas do catálogo.</summary>
    /// <example>
    /// <code>
    /// deckNameText.text = deck.Name;
    /// </code>
    /// </example>
    public sealed class PlayerDeck
    {
        private PlayerDeck(IPayloadReader deck)
        {
            Deck = deck.ReadDeckId("deck_id");
            Name = deck.ReadText("name");
            Cards = deck.ReadCardIdList("card_ids");
        }

        /// <summary>Identidade do deck; nome repetido é permitido.</summary>
        /// <example><code>DeckId chosen = deck.Deck;</code></example>
        public DeckId Deck { get; }

        /// <summary>Nome dado pelo jogador.</summary>
        /// <example><code>string name = deck.Name;</code></example>
        public string Name { get; }

        /// <summary>Cartas do catálogo, com repetições, na ordem do servidor.</summary>
        /// <example><code>int size = deck.Cards.Count;</code></example>
        public IReadOnlyList<CardId> Cards { get; }

        internal static PlayerDeck Read(IPayloadReader deck) => new PlayerDeck(deck);
    }
}
