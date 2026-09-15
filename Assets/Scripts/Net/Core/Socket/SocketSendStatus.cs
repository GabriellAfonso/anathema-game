#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Resultado de um envio de texto pelo socket.</summary>
    /// <example><code>if (outcome.Status == SocketSendStatus.NotOpen) QueueForLater(text);</code></example>
    internal enum SocketSendStatus
    {
        /// <summary>O texto saiu para o transporte.</summary>
        Sent,

        /// <summary>A conexão não estava aberta; nada foi enviado.</summary>
        NotOpen,

        /// <summary>O transporte falhou ao enviar.</summary>
        Failed,
    }
}
