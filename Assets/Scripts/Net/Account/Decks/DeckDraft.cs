#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Um deck a criar: nome e lista, os dois obrigatórios. A lista vai como veio; tamanho, cópias
    /// e cartas são validados pelo servidor, nunca aqui (FR-036).
    /// </summary>
    /// <example>
    /// <code>
    /// DeckDraft draft = new DeckDraft("Agro", cards);
    /// </code>
    /// </example>
    public sealed class DeckDraft
    {
        /// <summary>Cria o rascunho de envio; nome ou lista nulos lançam.</summary>
        /// <example><code>DeckDraft draft = new DeckDraft("Fumaça", spellDeckCards);</code></example>
        public DeckDraft(string name, IReadOnlyList<CardId> cards)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name), "deck draft name is null: expected the typed name, even if the server may refuse it");
            Cards = cards ?? throw new ArgumentNullException(nameof(cards), "deck draft cards is null: expected the list of card_ids to send");
        }

        /// <summary>Nome.</summary>
        /// <example><code>string name = draft.Name;</code></example>
        public string Name { get; }

        /// <summary>Cartas.</summary>
        /// <example><code>int size = draft.Cards.Count;</code></example>
        public IReadOnlyList<CardId> Cards { get; }
    }
}
