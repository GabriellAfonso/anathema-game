#if UNITY_ANDROID && !UNITY_EDITOR
#nullable enable
using Anathema.Net.Core;
using UnityEngine;
using UnityEngine.Android;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Guarda do refresh token no Android: AES-256-GCM com chave no Android Keystore, pelo plugin
    /// <c>Assets/Plugins/Android/RefreshTokenCipher.java</c> (research R4). Exceção Java vira
    /// resultado com o nome da exceção, nunca com o valor. Chamado na thread principal, que já está
    /// anexada à JVM.
    /// </summary>
    /// <example>
    /// <code>
    /// IRefreshTokenVault vault = new AndroidKeystoreRefreshTokenVault();
    /// </code>
    /// </example>
    public sealed class AndroidKeystoreRefreshTokenVault : IRefreshTokenVault
    {
        private const string CipherClass = "com.anathema.net.RefreshTokenCipher";

        /// <summary>Lê e decifra o token.</summary>
        /// <example><code>VaultReadOutcome stored = vault.Read();</code></example>
        public VaultReadOutcome Read()
        {
            try
            {
                using AndroidJavaClass cipher = new AndroidJavaClass(CipherClass);
                string? text = cipher.CallStatic<string>("read", AndroidApplication.currentContext);
                return string.IsNullOrWhiteSpace(text) ? VaultReadOutcome.Empty() : VaultReadOutcome.Found(new RefreshToken(text!));
            }
            catch (AndroidJavaException failure)
            {
                return VaultReadOutcome.Unreadable("android_error=" + JavaExceptionName(failure));
            }
        }

        /// <summary>Cifra e grava o token.</summary>
        /// <example><code>VaultWriteOutcome saved = vault.Save(refresh);</code></example>
        public VaultWriteOutcome Save(RefreshToken token)
        {
            try
            {
                using AndroidJavaClass cipher = new AndroidJavaClass(CipherClass);
                cipher.CallStatic("save", AndroidApplication.currentContext, token.RevealForRequest());
                return VaultWriteOutcome.Saved();
            }
            catch (AndroidJavaException failure)
            {
                return VaultWriteOutcome.Failed("android_error=" + JavaExceptionName(failure));
            }
        }

        /// <summary>Apaga o arquivo; a chave fica no Keystore para a próxima gravação.</summary>
        /// <example><code>vault.Delete();</code></example>
        public void Delete()
        {
            try
            {
                using AndroidJavaClass cipher = new AndroidJavaClass(CipherClass);
                cipher.CallStatic("delete", AndroidApplication.currentContext);
            }
            catch (AndroidJavaException)
            {
                // Apagar é melhor esforço: um arquivo que sobrou é lido como ilegível e apagado de novo.
            }
        }

        private static string JavaExceptionName(AndroidJavaException failure)
        {
            string message = failure.Message ?? string.Empty;
            int colon = message.IndexOf(':');
            return colon > 0 ? message.Substring(0, colon) : "AndroidJavaException";
        }
    }
}
#endif
