#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Identificador de carta no catálogo. Só serve para consultar nome, custo e efeito: toda
    /// jogada cita a cópia na partida, <see cref="CardInstanceId"/> (constituição, princípio
    /// III). No JSON viaja como o inteiro cru de <c>card_id</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// CardId card = item.ReadCardId("card_id");
    /// </code>
    /// </example>
    public readonly struct CardId : IEquatable<CardId>
    {
        /// <summary>Cria o identificador; valor menor que 1 lança.</summary>
        /// <example><code>CardId potion = new CardId(1004);</code></example>
        public CardId(long value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"card_id is {value}: expected a positive integer");

            Value = value;
        }

        /// <summary>Inteiro cru, como no JSON.</summary>
        /// <example><code>long raw = card.Value;</code></example>
        public long Value { get; }

        /// <summary>Mesma carta do catálogo.</summary>
        /// <example><code>bool same = card.Equals(other);</code></example>
        public bool Equals(CardId other) => Value == other.Value;

        /// <summary>Mesma carta do catálogo.</summary>
        /// <example><code>bool same = card.Equals((object)other);</code></example>
        public override bool Equals(object? obj) => obj is CardId other && Equals(other);

        /// <summary>Hash do valor.</summary>
        /// <example><code>int hash = card.GetHashCode();</code></example>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = card.ToString(); // card_id=1004</code></example>
        public override string ToString() => $"card_id={Value}";

        /// <summary>Mesma carta.</summary>
        /// <example><code>bool same = card == other;</code></example>
        public static bool operator ==(CardId left, CardId right) => left.Equals(right);

        /// <summary>Cartas diferentes.</summary>
        /// <example><code>bool different = card != other;</code></example>
        public static bool operator !=(CardId left, CardId right) => !left.Equals(right);
    }
}
