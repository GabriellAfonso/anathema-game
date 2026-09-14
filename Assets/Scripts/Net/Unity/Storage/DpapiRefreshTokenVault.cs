#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#nullable enable
using System;
using System.IO;
using System.Text;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Guarda do refresh token no Windows (player e editor): texto cifrado com a DPAPI do usuário
    /// atual e uma entropia do app, num arquivo por slot (research R3). Gravação atômica: um app
    /// morto no meio não deixa arquivo pela metade. Nenhuma operação lança.
    /// </summary>
    /// <example>
    /// <code>
    /// IRefreshTokenVault vault = new DpapiRefreshTokenVault(Path.Combine(Application.persistentDataPath, "account"), slot);
    /// </code>
    /// </example>
    public sealed class DpapiRefreshTokenVault : IRefreshTokenVault
    {
        // Sem entropia, qualquer programa do mesmo usuário decifraria o arquivo só chamando a DPAPI.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("anathema.refresh_token.v1");
        private readonly string filePath;

        /// <summary>Cria a guarda na pasta, com o arquivo do slot.</summary>
        /// <example><code>DpapiRefreshTokenVault vault = new DpapiRefreshTokenVault(directory, RefreshTokenVaultSlot.ForPlayer());</code></example>
        public DpapiRefreshTokenVault(string directory, RefreshTokenVaultSlot slot)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException($"vault directory is '{directory}': expected a writable folder such as persistentDataPath/account", nameof(directory));

            filePath = Path.Combine(directory, $"refresh_token.{(slot ?? throw new ArgumentNullException(nameof(slot), "vault slot is null: expected RefreshTokenVaultSlot.ForPlayer or ForEditorProject")).Name}.bin");
        }

        /// <summary>Lê e decifra o token do slot.</summary>
        /// <example><code>VaultReadOutcome stored = vault.Read();</code></example>
        public VaultReadOutcome Read()
        {
            if (!File.Exists(filePath))
                return VaultReadOutcome.Empty();

            try
            {
                return Decrypt(File.ReadAllBytes(filePath));
            }
            catch (Exception io) when (io is IOException || io is UnauthorizedAccessException)
            {
                return VaultReadOutcome.Unreadable("io_error=" + io.GetType().Name);
            }
        }

        /// <summary>Cifra e grava o token, substituindo o anterior.</summary>
        /// <example><code>VaultWriteOutcome saved = vault.Save(refresh);</code></example>
        public VaultWriteOutcome Save(RefreshToken token)
        {
            if (!DpapiNative.TryProtect(Encoding.UTF8.GetBytes(token.RevealForRequest()), Entropy, out byte[] cipher, out int error))
                return VaultWriteOutcome.Failed($"dpapi_error={error}");

            try
            {
                WriteAtomically(cipher);
                return VaultWriteOutcome.Saved();
            }
            catch (Exception io) when (io is IOException || io is UnauthorizedAccessException)
            {
                return VaultWriteOutcome.Failed("io_error=" + io.GetType().Name);
            }
        }

        /// <summary>Apaga o arquivo do slot e um temporário que tenha sobrado.</summary>
        /// <example><code>vault.Delete();</code></example>
        public void Delete()
        {
            DeleteQuietly(filePath);
            DeleteQuietly(filePath + ".tmp");
        }

        private static VaultReadOutcome Decrypt(byte[] cipher)
        {
            if (!DpapiNative.TryUnprotect(cipher, Entropy, out byte[] plain, out int error))
                return VaultReadOutcome.Unreadable($"dpapi_error={error}");

            string text = Encoding.UTF8.GetString(plain);
            return string.IsNullOrWhiteSpace(text) ? VaultReadOutcome.Unreadable("blank_after_decrypt") : VaultReadOutcome.Found(new RefreshToken(text));
        }

        private void WriteAtomically(byte[] cipher)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string temporary = filePath + ".tmp";
            File.WriteAllBytes(temporary, cipher);
            if (File.Exists(filePath))
                File.Replace(temporary, filePath, null);
            else
                File.Move(temporary, filePath);
        }

        private static void DeleteQuietly(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception io) when (io is IOException || io is UnauthorizedAccessException)
            {
                // Apagar é melhor esforço: um arquivo preso é lido como ilegível e apagado de novo depois.
            }
        }
    }
}
#endif
