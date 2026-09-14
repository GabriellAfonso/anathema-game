#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Guarda segura do refresh token no aparelho: Android Keystore no Android, DPAPI no
    /// Windows (constituição, "Technology And Platform Constraints"). Nunca texto puro, nunca
    /// <c>PlayerPrefs</c>. Chamada na thread principal; nenhuma operação lança.
    /// </summary>
    /// <example>
    /// <code>
    /// vault.Save(refresh);
    /// VaultReadOutcome stored = vault.Read();
    /// vault.Delete();
    /// </code>
    /// </example>
    public interface IRefreshTokenVault
    {
        /// <summary>Lê o token guardado.</summary>
        /// <example><code>VaultReadOutcome stored = vault.Read();</code></example>
        VaultReadOutcome Read();

        /// <summary>Grava o token, substituindo o anterior.</summary>
        /// <example><code>VaultWriteOutcome saved = vault.Save(refresh);</code></example>
        VaultWriteOutcome Save(RefreshToken token);

        /// <summary>Apaga o token; sem nada guardado, não faz nada.</summary>
        /// <example><code>vault.Delete();</code></example>
        void Delete();
    }
}
