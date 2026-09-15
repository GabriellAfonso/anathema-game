#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Por que a conexão parou de tentar (FR-022, FR-025).</summary>
    /// <example><code>if (status.Suspension == SuspensionReason.NoNetwork) ShowOffline();</code></example>
    internal enum SuspensionReason
    {
        /// <summary>O app está em segundo plano.</summary>
        Background,

        /// <summary>O aparelho está sem rede.</summary>
        NoNetwork,
    }
}
