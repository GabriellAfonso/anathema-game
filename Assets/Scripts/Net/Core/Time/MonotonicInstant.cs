#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Ponto no relógio monotônico do aparelho. Só serve para medir duração: não
    /// carrega hora absoluta, porque o relógio da vez conta a partir do instante
    /// em que o frame chegou, nunca pela hora de parede (constituição, princípio II).
    /// </summary>
    /// <example>
    /// <code>
    /// MonotonicInstant sentAt = clock.Now;
    /// TimeSpan elapsed = clock.Now - sentAt;
    /// </code>
    /// </example>
    public readonly struct MonotonicInstant : IEquatable<MonotonicInstant>, IComparable<MonotonicInstant>
    {
        /// <summary>Cria o instante a partir de ticks de 100 ns numa origem fixa por execução.</summary>
        /// <example><code>MonotonicInstant start = new MonotonicInstant(0);</code></example>
        public MonotonicInstant(long ticks)
        {
            Ticks = ticks;
        }

        /// <summary>Ticks de 100 ns desde a origem arbitrária do relógio.</summary>
        /// <example><code>long raw = clock.Now.Ticks;</code></example>
        public long Ticks { get; }

        /// <summary>Duração entre dois instantes do mesmo relógio.</summary>
        /// <example><code>TimeSpan awayFor = returnedAt - leftAt;</code></example>
        public static TimeSpan operator -(MonotonicInstant later, MonotonicInstant earlier)
        {
            return TimeSpan.FromTicks(later.Ticks - earlier.Ticks);
        }

        /// <summary>Instante deslocado por uma duração.</summary>
        /// <example><code>MonotonicInstant deadline = clock.Now.Add(TimeSpan.FromSeconds(5));</code></example>
        public MonotonicInstant Add(TimeSpan duration)
        {
            return new MonotonicInstant(Ticks + duration.Ticks);
        }

        /// <summary>Mesmo ponto no relógio.</summary>
        /// <example><code>bool same = first.Equals(second);</code></example>
        public bool Equals(MonotonicInstant other) => Ticks == other.Ticks;

        /// <summary>Mesmo ponto no relógio.</summary>
        /// <example><code>bool same = first.Equals((object)second);</code></example>
        public override bool Equals(object? obj) => obj is MonotonicInstant other && Equals(other);

        /// <summary>Hash dos ticks.</summary>
        /// <example><code>int hash = instant.GetHashCode();</code></example>
        public override int GetHashCode() => Ticks.GetHashCode();

        /// <summary>Ordena pelos ticks.</summary>
        /// <example><code>bool earlier = first.CompareTo(second) &lt; 0;</code></example>
        public int CompareTo(MonotonicInstant other) => Ticks.CompareTo(other.Ticks);

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = instant.ToString(); // monotonic_ticks=42</code></example>
        public override string ToString() => $"monotonic_ticks={Ticks}";

        /// <summary>Mesmo ponto no relógio.</summary>
        /// <example><code>bool same = first == second;</code></example>
        public static bool operator ==(MonotonicInstant left, MonotonicInstant right) => left.Equals(right);

        /// <summary>Pontos diferentes no relógio.</summary>
        /// <example><code>bool moved = first != second;</code></example>
        public static bool operator !=(MonotonicInstant left, MonotonicInstant right) => !left.Equals(right);

        /// <summary>O da esquerda veio antes.</summary>
        /// <example><code>bool earlier = first &lt; second;</code></example>
        public static bool operator <(MonotonicInstant left, MonotonicInstant right) => left.Ticks < right.Ticks;

        /// <summary>O da esquerda veio depois.</summary>
        /// <example><code>bool later = first &gt; second;</code></example>
        public static bool operator >(MonotonicInstant left, MonotonicInstant right) => left.Ticks > right.Ticks;

        /// <summary>O da esquerda não veio depois.</summary>
        /// <example><code>bool notLater = first &lt;= second;</code></example>
        public static bool operator <=(MonotonicInstant left, MonotonicInstant right) => left.Ticks <= right.Ticks;

        /// <summary>O da esquerda não veio antes.</summary>
        /// <example><code>bool notEarlier = first &gt;= second;</code></example>
        public static bool operator >=(MonotonicInstant left, MonotonicInstant right) => left.Ticks >= right.Ticks;
    }
}
