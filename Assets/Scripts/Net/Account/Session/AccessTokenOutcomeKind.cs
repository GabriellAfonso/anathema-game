#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>O que a porta de token válido conseguiu entregar.</summary>
    /// <example><code>if (outcome.Kind == AccessTokenOutcomeKind.Valid) OpenSocket(outcome.Token!);</code></example>
    public enum AccessTokenOutcomeKind
    {
        /// <summary>Um token fora da margem de renovação.</summary>
        Valid,

        /// <summary>Sem sessão, ou sessão expirada.</summary>
        SessionUnavailable,

        /// <summary>Precisava renovar e a renovação não saiu; a sessão continua.</summary>
        Unavailable,
    }
}
