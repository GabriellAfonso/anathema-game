#nullable enable
using Anathema.Net.Core;
using UnityEngine;
#if !(UNITY_ANDROID && !UNITY_EDITOR)
using System.IO;
#endif

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Escolhe a guarda segura do alvo: Android Keystore no aparelho, DPAPI no Windows e no editor
    /// (constituição, "Technology And Platform Constraints"). No editor, um slot por projeto, para os
    /// jogadores virtuais do Multiplayer Play Mode não dividirem sessão (FR-024). Quem compõe pode dar um slot
    /// nomeado, como os dois clientes da prova final (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// IRefreshTokenVault vault = PlatformRefreshTokenVault.Create(log);
    /// </code>
    /// </example>
    internal static class PlatformRefreshTokenVault
    {
        /// <summary>A guarda do alvo atual no slot dado (nulo: o da plataforma); registra qual foi escolhida, sem caminho de usuário.</summary>
        /// <example><code>IRefreshTokenVault vault = PlatformRefreshTokenVault.Create(log, RefreshTokenVaultSlot.Named("proof-p1"));</code></example>
        public static IRefreshTokenVault Create(IClientLog log, RefreshTokenVaultSlot? slot = null)
        {
            RefreshTokenVaultSlot chosen = slot ?? CurrentSlot();
#if UNITY_ANDROID && !UNITY_EDITOR
            log.Info("refresh_token_vault_selected", new LogField("kind", "android_keystore"), new LogField("slot", chosen.Name));
            return new AndroidKeystoreRefreshTokenVault(chosen);
#else
            log.Info("refresh_token_vault_selected", new LogField("kind", "dpapi"), new LogField("slot", chosen.Name));
            return new DpapiRefreshTokenVault(Path.Combine(Application.persistentDataPath, "account"), chosen);
#endif
        }

        private static RefreshTokenVaultSlot CurrentSlot()
        {
            return Application.isEditor ? RefreshTokenVaultSlot.ForEditorProject(Application.dataPath) : RefreshTokenVaultSlot.ForPlayer();
        }
    }
}
