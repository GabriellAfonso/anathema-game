#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Identificador de partida. No JSON viaja como o texto cru de <c>match_id</c>
    /// (um UUID); tipo próprio para não confundir com qualquer outro texto.
    /// </summary>
    /// <example>
    /// <code>
    /// MatchId match = payload.ReadMatchId("match_id");
    /// Uri url = new Uri($"{wsBase}/ws/match/?matchId={match.Value}&amp;token={token}");
    /// </code>
    /// </example>
    public readonly struct MatchId : IEquatable<MatchId>
    {
        private readonly string? value;

        /// <summary>Cria o identificador; vazio ou com espaço nas pontas lança.</summary>
        /// <example><code>MatchId match = new MatchId("0b7c…");</code></example>
        public MatchId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() != value)
                throw new ArgumentException($"match_id is '{value}': expected non-empty text without surrounding spaces", nameof(value));

            this.value = value;
        }

        /// <summary>Texto cru, como no JSON.</summary>
        /// <example><code>string raw = match.Value;</code></example>
        public string Value => value ?? string.Empty;

        /// <summary>Mesma partida (comparação ordinal).</summary>
        /// <example><code>bool same = current.Equals(found);</code></example>
        public bool Equals(MatchId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>Mesma partida.</summary>
        /// <example><code>bool same = current.Equals((object)found);</code></example>
        public override bool Equals(object? obj) => obj is MatchId other && Equals(other);

        /// <summary>Hash ordinal do texto.</summary>
        /// <example><code>int hash = match.GetHashCode();</code></example>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = match.ToString(); // match_id=0b7c…</code></example>
        public override string ToString() => $"match_id={Value}";

        /// <summary>Mesma partida.</summary>
        /// <example><code>bool same = current == found;</code></example>
        public static bool operator ==(MatchId left, MatchId right) => left.Equals(right);

        /// <summary>Partidas diferentes.</summary>
        /// <example><code>bool stale = current != found;</code></example>
        public static bool operator !=(MatchId left, MatchId right) => !left.Equals(right);
    }
}
