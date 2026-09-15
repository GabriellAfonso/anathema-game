#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Resultado de ler a guarda segura. Falha de leitura é resultado, não exceção: a sessão
    /// trata "ilegível" como "nada guardado" e segue (FR-023).
    /// </summary>
    /// <example>
    /// <code>
    /// VaultReadOutcome stored = vault.Read();
    /// if (stored.Kind == VaultReadKind.Unreadable) vault.Delete();
    /// </code>
    /// </example>
    internal sealed class VaultReadOutcome
    {
        private VaultReadOutcome(VaultReadKind kind, RefreshToken? token, string detail)
        {
            Kind = kind;
            Token = token;
            Detail = detail;
        }

        /// <summary>O que foi encontrado.</summary>
        /// <example><code>VaultReadKind kind = stored.Kind;</code></example>
        public VaultReadKind Kind { get; }

        /// <summary>O token, só quando <see cref="VaultReadKind.Found"/>.</summary>
        /// <example><code>RefreshToken? refresh = stored.Token;</code></example>
        public RefreshToken? Token { get; }

        /// <summary>Motivo quando ilegível; vazio nos outros casos. Nunca contém o valor.</summary>
        /// <example><code>log.Warning("vault_unreadable", new LogField("detail", stored.Detail));</code></example>
        public string Detail { get; }

        /// <summary>Um token legível.</summary>
        /// <example><code>return VaultReadOutcome.Found(new RefreshToken(text));</code></example>
        public static VaultReadOutcome Found(RefreshToken token)
        {
            return new VaultReadOutcome(VaultReadKind.Found, token ?? throw new ArgumentNullException(nameof(token), "found refresh token is null: expected the token read from the vault"), string.Empty);
        }

        /// <summary>Nada guardado.</summary>
        /// <example><code>return VaultReadOutcome.Empty();</code></example>
        public static VaultReadOutcome Empty() => new VaultReadOutcome(VaultReadKind.Empty, null, string.Empty);

        /// <summary>Algo guardado que não deu para ler.</summary>
        /// <example><code>return VaultReadOutcome.Unreadable("dpapi_error=13");</code></example>
        public static VaultReadOutcome Unreadable(string detail)
        {
            return new VaultReadOutcome(VaultReadKind.Unreadable, null, detail ?? throw new ArgumentNullException(nameof(detail), "unreadable vault detail is null: expected why the value could not be read"));
        }

        /// <summary>Forma para log, sem o token.</summary>
        /// <example><code>string text = stored.ToString(); // Unreadable: dpapi_error=13</code></example>
        public override string ToString() => Detail.Length == 0 ? Kind.ToString() : $"{Kind}: {Detail}";
    }
}
