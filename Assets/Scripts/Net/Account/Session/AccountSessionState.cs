#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Em que ponto está a sessão de conta.</summary>
    /// <example><code>if (session.State == AccountSessionState.Expired) ShowLogin();</code></example>
    public enum AccountSessionState
    {
        /// <summary>Ninguém entrou, ou o jogador saiu.</summary>
        SignedOut,

        /// <summary>Um login está em curso.</summary>
        SigningIn,

        /// <summary>Há tokens em memória.</summary>
        SignedIn,

        /// <summary>O servidor recusou o refresh token; é preciso entrar de novo.</summary>
        Expired,
    }
}
