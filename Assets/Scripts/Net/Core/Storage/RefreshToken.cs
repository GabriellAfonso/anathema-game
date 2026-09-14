#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Refresh token emitido no login e guardado no aparelho. Tipo próprio para o texto não
    /// ir parar num log por engano: <see cref="ToString"/> mascara, e só quem monta o pedido
    /// HTTP ou grava a guarda chama <see cref="RevealForRequest"/> (FR-040).
    /// </summary>
    /// <example>
    /// <code>
    /// RefreshToken refresh = new RefreshToken(body.ReadText("refresh"));
    /// </code>
    /// </example>
    public sealed class RefreshToken : IEquatable<RefreshToken>
    {
        private readonly string text;

        /// <summary>Cria o token; texto vazio ou só com espaços lança, sem ecoar o texto.</summary>
        /// <example><code>RefreshToken refresh = new RefreshToken(stored);</code></example>
        public RefreshToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException($"refresh token is {(text == null ? "null" : $"{text.Length} blank characters")}: expected the non-empty token issued by the server", nameof(text));

            this.text = text;
        }

        /// <summary>O texto do token, só para o corpo do pedido ou para a guarda.</summary>
        /// <example><code>writer.WriteText("refresh", refresh.RevealForRequest());</code></example>
        public string RevealForRequest() => text;

        /// <summary>Mesmo token.</summary>
        /// <example><code>bool same = stored.Equals(issued);</code></example>
        public bool Equals(RefreshToken? other) => other != null && string.Equals(text, other.text, StringComparison.Ordinal);

        /// <summary>Mesmo token.</summary>
        /// <example><code>bool same = stored.Equals((object)issued);</code></example>
        public override bool Equals(object? obj) => obj is RefreshToken other && Equals(other);

        /// <summary>Hash do texto.</summary>
        /// <example><code>int hash = refresh.GetHashCode();</code></example>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(text);

        /// <summary>Forma para log, sempre mascarada.</summary>
        /// <example><code>string text = refresh.ToString(); // refresh_token=&lt;redacted&gt;</code></example>
        public override string ToString() => "refresh_token=<redacted>";
    }
}
