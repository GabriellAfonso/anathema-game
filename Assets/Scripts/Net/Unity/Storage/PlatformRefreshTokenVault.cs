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
    /// jogadores virtuais do Multiplayer Play Mode não dividirem sessão (FR-024).
    /// </summary>
    /// <example>
    /// <code>
    /// IRefreshTokenVault vault = PlatformRefreshTokenVault.Create(log);
    /// </code>
    /// </example>
    public static class PlatformRefreshTokenVault
    {
        /// <summary>A guarda do alvo atual; registra qual foi escolhida, sem caminho de usuário.</summary>
        /// <example><code>IRefreshTokenVault vault = PlatformRefreshTokenVault.Create(log);</code></example>
        public static IRefreshTokenVault Create(IClientLog log)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            log.Info("refresh_token_vault_selected", new LogField("kind", "android_keystore"));
            return new AndroidKeystoreRefreshTokenVault();
#else
            RefreshTokenVaultSlot slot = CurrentSlot();
            log.Info("refresh_token_vault_selected", new LogField("kind", "dpapi"), new LogField("slot", slot.Name));
            return new DpapiRefreshTokenVault(Path.Combine(Application.persistentDataPath, "account"), slot);
#endif
        }

        private static RefreshTokenVaultSlot CurrentSlot()
        {
            return Application.isEditor ? RefreshTokenVaultSlot.ForEditorProject(Application.dataPath) : RefreshTokenVaultSlot.ForPlayer();
        }
    }
}
