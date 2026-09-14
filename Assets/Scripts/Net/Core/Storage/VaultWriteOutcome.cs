#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Resultado de gravar na guarda segura. Falha não é exceção: o login vale em memória e só
    /// a retomada seguinte deixa de achar o token (FR-023).
    /// </summary>
    /// <example>
    /// <code>
    /// VaultWriteOutcome saved = vault.Save(refresh);
    /// if (saved.Kind == VaultWriteKind.Failed) log.Warning("vault_save_failed", new LogField("detail", saved.Detail));
    /// </code>
    /// </example>
    public sealed class VaultWriteOutcome
    {
        private VaultWriteOutcome(VaultWriteKind kind, string detail)
        {
            Kind = kind;
            Detail = detail;
        }

        /// <summary>Como terminou.</summary>
        /// <example><code>VaultWriteKind kind = saved.Kind;</code></example>
        public VaultWriteKind Kind { get; }

        /// <summary>Motivo da falha; vazio quando gravou. Nunca contém o valor.</summary>
        /// <example><code>string why = saved.Detail;</code></example>
        public string Detail { get; }

        /// <summary>Gravou.</summary>
        /// <example><code>return VaultWriteOutcome.Saved();</code></example>
        public static VaultWriteOutcome Saved() => new VaultWriteOutcome(VaultWriteKind.Saved, string.Empty);

        /// <summary>Não gravou.</summary>
        /// <example><code>return VaultWriteOutcome.Failed("io_error=UnauthorizedAccessException");</code></example>
        public static VaultWriteOutcome Failed(string detail)
        {
            return new VaultWriteOutcome(VaultWriteKind.Failed, detail ?? throw new ArgumentNullException(nameof(detail), "vault write failure detail is null: expected why the token was not saved"));
        }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = saved.ToString(); // Failed: io_error=IOException</code></example>
        public override string ToString() => Detail.Length == 0 ? Kind.ToString() : $"{Kind}: {Detail}";
    }
}
