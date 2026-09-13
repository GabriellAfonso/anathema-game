#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Log de depuração da camada de rede. O núcleo não conhece <c>Debug</c> do
    /// motor; só o adaptador que implementa esta interface escreve no console.
    /// Seguro de qualquer thread.
    /// </summary>
    /// <example>
    /// <code>
    /// log.Write(new ClientLogEntry(ClientLogLevel.Info, "socket_opened", fields));
    /// </code>
    /// </example>
    public interface IClientLog
    {
        /// <summary>Registra uma entrada.</summary>
        /// <example><code>log.Write(entry);</code></example>
        void Write(ClientLogEntry entry);
    }
}
