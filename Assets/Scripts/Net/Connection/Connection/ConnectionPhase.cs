#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Em que ponto do ciclo a conexão está (FR-011).</summary>
    /// <example><code>if (connection.Status.Phase == ConnectionPhase.WaitingRetry) ShowReconnecting();</code></example>
    public enum ConnectionPhase
    {
        /// <summary>Nunca conectou, ou saiu de propósito.</summary>
        Disconnected,

        /// <summary>Pedindo token ou no handshake.</summary>
        Connecting,

        /// <summary>Socket aberto.</summary>
        Connected,

        /// <summary>Caiu; esperando a vez da próxima tentativa.</summary>
        WaitingRetry,

        /// <summary>O servidor recusou o token; renovando antes de reabrir.</summary>
        RenewingToken,

        /// <summary>Sem socket aberto e sem tentar: app em segundo plano ou sem rede.</summary>
        Suspended,

        /// <summary>Desistiu. Só um <c>Connect</c> explícito recomeça.</summary>
        GaveUp,
    }
}
