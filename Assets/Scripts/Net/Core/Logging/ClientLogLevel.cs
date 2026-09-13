#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Gravidade de um registro de depuração.</summary>
    /// <example><code>log.Write(new ClientLogEntry(ClientLogLevel.Warning, "socket_errored", fields));</code></example>
    public enum ClientLogLevel
    {
        /// <summary>Detalhe útil só ao investigar.</summary>
        Debug,

        /// <summary>Marco normal do fluxo.</summary>
        Info,

        /// <summary>Algo inesperado que a camada contornou.</summary>
        Warning,

        /// <summary>Falha que alguém precisa olhar.</summary>
        Error,
    }
}
