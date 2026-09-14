#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Por que não há token de acesso para usar.</summary>
    /// <example><code>if (failure.Session == SessionUnavailableKind.Expired) AskToSignInAgain();</code></example>
    public enum SessionUnavailableKind
    {
        /// <summary>Ninguém entrou, ou o jogador saiu.</summary>
        NoSession,

        /// <summary>O servidor recusou o refresh token: é preciso entrar de novo.</summary>
        Expired,
    }
}
