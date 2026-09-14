#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Identificador de deck do jogador. Tipo próprio para que passar um <see cref="CardId"/>
    /// no lugar seja erro de compilação (constituição, princípio III). No JSON viaja como o
    /// inteiro cru de <c>deck_id</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// DeckId chosen = body.ReadDeckId("deck_id");
    /// </code>
    /// </example>
    public readonly struct DeckId : IEquatable<DeckId>
    {
        /// <summary>Cria o identificador; valor menor que 1 lança.</summary>
        /// <example><code>DeckId deck = new DeckId(4);</code></example>
        public DeckId(long value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"deck_id is {value}: expected a positive integer");

            Value = value;
        }

        /// <summary>Inteiro cru, como no JSON.</summary>
        /// <example><code>long raw = deck.Value;</code></example>
        public long Value { get; }

        /// <summary>Mesmo deck.</summary>
        /// <example><code>bool same = chosen.Equals(listed);</code></example>
        public bool Equals(DeckId other) => Value == other.Value;

        /// <summary>Mesmo deck.</summary>
        /// <example><code>bool same = chosen.Equals((object)listed);</code></example>
        public override bool Equals(object? obj) => obj is DeckId other && Equals(other);

        /// <summary>Hash do valor.</summary>
        /// <example><code>int hash = deck.GetHashCode();</code></example>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = deck.ToString(); // deck_id=4</code></example>
        public override string ToString() => $"deck_id={Value}";

        /// <summary>Mesmo deck.</summary>
        /// <example><code>bool same = chosen == listed;</code></example>
        public static bool operator ==(DeckId left, DeckId right) => left.Equals(right);

        /// <summary>Decks diferentes.</summary>
        /// <example><code>bool other = chosen != listed;</code></example>
        public static bool operator !=(DeckId left, DeckId right) => !left.Equals(right);
    }
}
