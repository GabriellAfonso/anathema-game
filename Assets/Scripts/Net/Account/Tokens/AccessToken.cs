#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Token de acesso com a validade medida sem o relógio do aparelho: a vida útil é
    /// <c>exp - iat</c> do próprio JWT (os dois são hora do servidor) e conta a partir do
    /// instante monotônico em que a resposta chegou (FR-009; research R1). Só
    /// <see cref="AccessTokenReader"/> constrói.
    /// </summary>
    /// <example>
    /// <code>
    /// if (token.NeedsRenewal(clock.Now, timing.RenewalMargin)) await renewal.RenewAsync();
    /// </code>
    /// </example>
    internal sealed class AccessToken
    {
        private readonly string text;

        internal AccessToken(string text, UserId owner, TimeSpan lifetime, MonotonicInstant arrivedAt)
        {
            this.text = text;
            Owner = owner;
            Lifetime = lifetime;
            ArrivedAt = arrivedAt;
        }

        /// <summary>Dono do token, do claim <c>user_id</c>.</summary>
        /// <example><code>UserId self = token.Owner;</code></example>
        public UserId Owner { get; }

        /// <summary>Vida útil, <c>exp - iat</c>.</summary>
        /// <example><code>TimeSpan lifetime = token.Lifetime; // 00:05:00</code></example>
        public TimeSpan Lifetime { get; }

        /// <summary>Instante monotônico em que a resposta com o token chegou.</summary>
        /// <example><code>MonotonicInstant arrived = token.ArrivedAt;</code></example>
        public MonotonicInstant ArrivedAt { get; }

        /// <summary>Instante monotônico em que o token vence, pela conta do cliente.</summary>
        /// <example><code>TimeSpan left = token.ExpiresAt - clock.Now;</code></example>
        public MonotonicInstant ExpiresAt => ArrivedAt.Add(Lifetime);

        /// <summary>Venceu ou falta menos que a margem.</summary>
        /// <example><code>bool renew = token.NeedsRenewal(clock.Now, TimeSpan.FromSeconds(30));</code></example>
        public bool NeedsRenewal(MonotonicInstant now, TimeSpan margin) => now >= ExpiresAt.Add(-margin);

        /// <summary>O texto do JWT, só para o cabeçalho <c>Authorization</c> ou o <c>?token=</c>.</summary>
        /// <example><code>headers["Authorization"] = "Bearer " + token.RevealForRequest();</code></example>
        public string RevealForRequest() => text;

        /// <summary>Forma para log, sem o texto.</summary>
        /// <example><code>string text = token.ToString(); // access_token=&lt;redacted&gt; user_id=7</code></example>
        public override string ToString() => $"access_token=<redacted> {Owner}";

        internal bool HasSameTextAs(AccessToken other) => string.Equals(text, other.text, StringComparison.Ordinal);
    }
}
