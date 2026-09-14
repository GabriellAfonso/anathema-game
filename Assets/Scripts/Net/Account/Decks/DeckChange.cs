#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O que trocar num deck: nome, lista inteira, ou os dois. Sem nenhum dos dois não há pedido a
    /// fazer; isso é forma do comando, não validação de deck. A lista substitui a anterior inteira.
    /// </summary>
    /// <example>
    /// <code>
    /// DeckChange rename = new DeckChange(name: "Agro v2");
    /// </code>
    /// </example>
    public sealed class DeckChange
    {
        /// <summary>Cria a mudança; nome e lista nulos ao mesmo tempo lançam.</summary>
        /// <example><code>DeckChange swap = new DeckChange(cards: newCards);</code></example>
        public DeckChange(string? name = null, IReadOnlyList<CardId>? cards = null)
        {
            if (name == null && cards == null)
                throw new ArgumentException("deck change has name null and cards null: expected a new name, a new card list, or both");

            Name = name;
            Cards = cards;
        }

        /// <summary>Nome novo, ou nulo para manter.</summary>
        /// <example><code>bool renaming = change.Name != null;</code></example>
        public string? Name { get; }

        /// <summary>Lista nova, ou nula para manter.</summary>
        /// <example><code>bool swapping = change.Cards != null;</code></example>
        public IReadOnlyList<CardId>? Cards { get; }
    }
}
