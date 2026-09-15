#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Por que o jogador está deslogado: a tela de login mostra aviso só na expiração.</summary>
    /// <example><code>expiredBanner.SetActive(client.State.SignedOutReason == SignedOutReason.SessionExpired);</code></example>
    public enum SignedOutReason
    {
        /// <summary>Composição recém-feita, ainda sem sessão.</summary>
        Startup,

        /// <summary>O jogador pediu para sair.</summary>
        SignedOut,

        /// <summary>O servidor recusou renovar a sessão, ou a conexão desistiu por falta de sessão.</summary>
        SessionExpired,
    }
}
