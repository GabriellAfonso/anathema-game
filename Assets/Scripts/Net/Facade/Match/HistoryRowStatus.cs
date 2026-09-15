#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Onde está a linha do histórico da partida que acabou.</summary>
    /// <example><code>spinner.SetActive(result.RowStatus == HistoryRowStatus.Fetching);</code></example>
    public enum HistoryRowStatus
    {
        /// <summary>Ainda buscando: o servidor grava a linha depois do último frame.</summary>
        Fetching,

        /// <summary>A linha chegou.</summary>
        Resolved,

        /// <summary>A linha não apareceu depois das tentativas; vale o desfecho do espelho.</summary>
        Unavailable,
    }
}
