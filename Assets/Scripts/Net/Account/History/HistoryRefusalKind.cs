#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Por que a página do histórico não veio.</summary>
    /// <example><code>if (refusal.Kind == HistoryRefusalKind.PastTheEnd) DisableNext();</code></example>
    public enum HistoryRefusalKind
    {
        /// <summary>404 numa página além da primeira.</summary>
        PastTheEnd,

        /// <summary>404 na primeira página: a conta não tem perfil.</summary>
        NoProfile,

        /// <summary>Outra resposta que o cliente não tipa.</summary>
        Unrecognized,
    }
}
