#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Recusa de uma página do histórico. O 404 do servidor tem o mesmo corpo nos dois casos; quem
    /// distingue é o número da página pedida (FR-039).
    /// </summary>
    /// <example>
    /// <code>
    /// if (outcome.Refusal?.Kind == HistoryRefusalKind.NoProfile) ShowSupport();
    /// </code>
    /// </example>
    public sealed class HistoryRefusal
    {
        private HistoryRefusal(HistoryRefusalKind kind, UnrecognizedRefusal? unrecognized)
        {
            Kind = kind;
            Unrecognized = unrecognized;
        }

        /// <summary>Qual recusa.</summary>
        /// <example><code>HistoryRefusalKind kind = refusal.Kind;</code></example>
        public HistoryRefusalKind Kind { get; }

        /// <summary>Status e trecho do corpo; só em <see cref="HistoryRefusalKind.Unrecognized"/>.</summary>
        /// <example><code>int? status = refusal.Unrecognized?.Status;</code></example>
        public UnrecognizedRefusal? Unrecognized { get; }

        /// <summary>Página além do fim.</summary>
        /// <example><code>return HistoryRefusal.PastTheEnd();</code></example>
        public static HistoryRefusal PastTheEnd() => new HistoryRefusal(HistoryRefusalKind.PastTheEnd, null);

        /// <summary>Conta sem perfil.</summary>
        /// <example><code>return HistoryRefusal.NoProfile();</code></example>
        public static HistoryRefusal NoProfile() => new HistoryRefusal(HistoryRefusalKind.NoProfile, null);

        /// <summary>Resposta não reconhecida.</summary>
        /// <example><code>return HistoryRefusal.FromUnrecognized(new UnrecognizedRefusal(500, body));</code></example>
        public static HistoryRefusal FromUnrecognized(UnrecognizedRefusal unrecognized)
        {
            return new HistoryRefusal(HistoryRefusalKind.Unrecognized, unrecognized ?? throw new ArgumentNullException(nameof(unrecognized), "unrecognized history refusal is null: expected status and body"));
        }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = refusal.ToString(); // PastTheEnd</code></example>
        public override string ToString() => Unrecognized == null ? Kind.ToString() : $"{Kind} {Unrecognized}";
    }
}
