#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Nome do arquivo da guarda dentro da pasta de dados. O Multiplayer Play Mode roda cada
    /// jogador virtual como um editor clonado com projeto próprio: o <c>Application.dataPath</c>
    /// muda, o <c>persistentDataPath</c> não. Sem slot, um jogador retomaria a sessão do outro
    /// (FR-024; research R3).
    /// </summary>
    /// <example>
    /// <code>
    /// RefreshTokenVaultSlot slot = Application.isEditor ? RefreshTokenVaultSlot.ForEditorProject(Application.dataPath) : RefreshTokenVaultSlot.ForPlayer();
    /// </code>
    /// </example>
    public sealed class RefreshTokenVaultSlot
    {
        private static readonly Regex AllowedName = new Regex("^[a-z0-9_-]{1,64}$");

        private RefreshTokenVaultSlot(string name)
        {
            Name = name;
        }

        /// <summary>Nome usado no arquivo.</summary>
        /// <example><code>string file = $"refresh_token.{slot.Name}.bin";</code></example>
        public string Name { get; }

        /// <summary>O único slot do jogo instalado.</summary>
        /// <example><code>RefreshTokenVaultSlot slot = RefreshTokenVaultSlot.ForPlayer();</code></example>
        public static RefreshTokenVaultSlot ForPlayer() => new RefreshTokenVaultSlot("player");

        /// <summary>Slot de um projeto do editor: <c>editor-</c> e 12 hex do SHA-256 do caminho.</summary>
        /// <example><code>RefreshTokenVaultSlot slot = RefreshTokenVaultSlot.ForEditorProject(Application.dataPath);</code></example>
        public static RefreshTokenVaultSlot ForEditorProject(string dataPath)
        {
            if (string.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException($"editor data path is '{dataPath}': expected Application.dataPath of the running project", nameof(dataPath));

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dataPath));
            return new RefreshTokenVaultSlot("editor-" + BitConverter.ToString(hash, 0, 6).Replace("-", string.Empty).ToLowerInvariant());
        }

        /// <summary>Slot com nome dado, para testes; só minúsculas, dígitos, <c>_</c> e <c>-</c>.</summary>
        /// <example><code>RefreshTokenVaultSlot slot = RefreshTokenVaultSlot.Named("live-test-1");</code></example>
        public static RefreshTokenVaultSlot Named(string name)
        {
            if (name != null && AllowedName.IsMatch(name))
                return new RefreshTokenVaultSlot(name);

            throw new ArgumentException($"vault slot name is '{name}': expected 1 to 64 characters among a-z, 0-9, '_' and '-'", nameof(name));
        }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = slot.ToString(); // vault_slot=player</code></example>
        public override string ToString() => $"vault_slot={Name}";
    }
}
