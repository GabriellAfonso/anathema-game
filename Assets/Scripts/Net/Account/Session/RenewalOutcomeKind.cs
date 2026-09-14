#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Como terminou uma renovação do token de acesso.</summary>
    /// <example><code>if (outcome.Kind == RenewalOutcomeKind.SessionExpired) ShowLogin();</code></example>
    public enum RenewalOutcomeKind
    {
        /// <summary>Token de acesso novo.</summary>
        Renewed,

        /// <summary>O servidor recusou o refresh token (401).</summary>
        SessionExpired,

        /// <summary>Não há sessão para renovar.</summary>
        NoSession,

        /// <summary>Não saiu, mas a sessão continua valendo.</summary>
        Unavailable,
    }
}
