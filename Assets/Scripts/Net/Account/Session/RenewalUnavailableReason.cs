#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Por que uma renovação não saiu, sem que a sessão tenha expirado (research R5).</summary>
    /// <example><code>if (renewal.Reason == RenewalUnavailableReason.Transport) RetryLater();</code></example>
    public enum RenewalUnavailableReason
    {
        /// <summary>Sem rede, host não resolvido, prazo esgotado.</summary>
        Transport,

        /// <summary>O servidor respondeu status diferente de 200 e de 401.</summary>
        ServerStatus,

        /// <summary>200 sem <c>access</c> legível.</summary>
        OutOfContract,
    }
}
