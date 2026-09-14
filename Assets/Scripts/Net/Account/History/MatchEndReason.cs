#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Como a partida acabou, pelo conjunto fechado de <c>end_reason</c> do contrato de histórico.</summary>
    /// <example><code>string label = row.EndReason == MatchEndReason.Forfeit ? "Desistência" : "Nexus zerado";</code></example>
    public enum MatchEndReason
    {
        /// <summary><c>nexus_depleted</c>.</summary>
        NexusDepleted,

        /// <summary><c>forfeit</c>.</summary>
        Forfeit,

        /// <summary>Valor fora do conjunto; o texto fica em <c>EndReasonText</c>.</summary>
        Unknown,
    }
}
