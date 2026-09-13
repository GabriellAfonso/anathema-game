#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Identificador de usuário. Tipo próprio para que passar um
    /// <see cref="CardInstanceId"/> no lugar seja erro de compilação
    /// (constituição, princípio III). No JSON viaja como o inteiro cru de <c>user_id</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// UserId self = payload.ReadObject("self").ReadUserId("user_id");
    /// </code>
    /// </example>
    public readonly struct UserId : IEquatable<UserId>
    {
        /// <summary>Cria o identificador; valor menor que 1 lança.</summary>
        /// <example><code>UserId player = new UserId(7);</code></example>
        public UserId(long value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"user_id is {value}: expected a positive integer");

            Value = value;
        }

        /// <summary>Inteiro cru, como no JSON.</summary>
        /// <example><code>long raw = userId.Value;</code></example>
        public long Value { get; }

        /// <summary>Mesmo usuário.</summary>
        /// <example><code>bool same = self.Equals(winner);</code></example>
        public bool Equals(UserId other) => Value == other.Value;

        /// <summary>Mesmo usuário.</summary>
        /// <example><code>bool same = self.Equals((object)winner);</code></example>
        public override bool Equals(object? obj) => obj is UserId other && Equals(other);

        /// <summary>Hash do valor.</summary>
        /// <example><code>int hash = userId.GetHashCode();</code></example>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = userId.ToString(); // user_id=7</code></example>
        public override string ToString() => $"user_id={Value}";

        /// <summary>Mesmo usuário.</summary>
        /// <example><code>bool won = outcome.Winner == self;</code></example>
        public static bool operator ==(UserId left, UserId right) => left.Equals(right);

        /// <summary>Usuários diferentes.</summary>
        /// <example><code>bool lost = outcome.Winner != self;</code></example>
        public static bool operator !=(UserId left, UserId right) => !left.Equals(right);
    }
}
