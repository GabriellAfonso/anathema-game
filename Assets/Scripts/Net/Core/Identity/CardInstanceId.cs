#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Identificador da cópia de uma carta dentro da partida. Toda jogada cita este,
    /// nunca o <c>card_id</c> do catálogo (constituição, princípio III). No JSON
    /// viaja como o inteiro cru de <c>card_instance_id</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// payload.WriteCardInstanceId("card_instance_id", chosen);
    /// </code>
    /// </example>
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>
    {
        /// <summary>Cria o identificador; valor negativo lança.</summary>
        /// <example><code>CardInstanceId copy = new CardInstanceId(3);</code></example>
        public CardInstanceId(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"card_instance_id is {value}: expected a non-negative integer");

            Value = value;
        }

        /// <summary>Inteiro cru, como no JSON.</summary>
        /// <example><code>long raw = copy.Value;</code></example>
        public long Value { get; }

        /// <summary>Mesma cópia.</summary>
        /// <example><code>bool same = attacker.Equals(target);</code></example>
        public bool Equals(CardInstanceId other) => Value == other.Value;

        /// <summary>Mesma cópia.</summary>
        /// <example><code>bool same = attacker.Equals((object)target);</code></example>
        public override bool Equals(object? obj) => obj is CardInstanceId other && Equals(other);

        /// <summary>Hash do valor.</summary>
        /// <example><code>int hash = copy.GetHashCode();</code></example>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = copy.ToString(); // card_instance_id=3</code></example>
        public override string ToString() => $"card_instance_id={Value}";

        /// <summary>Mesma cópia.</summary>
        /// <example><code>bool same = attacker == target;</code></example>
        public static bool operator ==(CardInstanceId left, CardInstanceId right) => left.Equals(right);

        /// <summary>Cópias diferentes.</summary>
        /// <example><code>bool other = attacker != target;</code></example>
        public static bool operator !=(CardInstanceId left, CardInstanceId right) => !left.Equals(right);
    }
}
