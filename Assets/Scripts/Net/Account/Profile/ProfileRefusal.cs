#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>Recusa de <c>GET /players/me/</c>: conta sem perfil, ou resposta não reconhecida.</summary>
    /// <example>
    /// <code>
    /// if (outcome.Refusal?.Kind == ProfileRefusalKind.ProfileMissing) ShowSupport();
    /// </code>
    /// </example>
    public sealed class ProfileRefusal
    {
        private ProfileRefusal(ProfileRefusalKind kind, UnrecognizedRefusal? unrecognized)
        {
            Kind = kind;
            Unrecognized = unrecognized;
        }

        /// <summary>Qual recusa.</summary>
        /// <example><code>ProfileRefusalKind kind = refusal.Kind;</code></example>
        public ProfileRefusalKind Kind { get; }

        /// <summary>Status e trecho do corpo; só em <see cref="ProfileRefusalKind.Unrecognized"/>.</summary>
        /// <example><code>int? status = refusal.Unrecognized?.Status;</code></example>
        public UnrecognizedRefusal? Unrecognized { get; }

        /// <summary>Conta sem perfil.</summary>
        /// <example><code>return ProfileRefusal.Missing();</code></example>
        public static ProfileRefusal Missing() => new ProfileRefusal(ProfileRefusalKind.ProfileMissing, null);

        /// <summary>Resposta não reconhecida.</summary>
        /// <example><code>return ProfileRefusal.FromUnrecognized(new UnrecognizedRefusal(500, body));</code></example>
        public static ProfileRefusal FromUnrecognized(UnrecognizedRefusal unrecognized)
        {
            return new ProfileRefusal(ProfileRefusalKind.Unrecognized, unrecognized ?? throw new ArgumentNullException(nameof(unrecognized), "unrecognized profile refusal is null: expected status and body"));
        }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = refusal.ToString(); // ProfileMissing</code></example>
        public override string ToString() => Unrecognized == null ? Kind.ToString() : $"{Kind} {Unrecognized}";
    }
}
