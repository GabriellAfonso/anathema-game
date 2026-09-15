#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Cria a conexão da vez. Uma instância de <see cref="IWebSocket"/> é uma conexão, então quem
    /// reconecta precisa de uma nova a cada tentativa
    /// (specs/003-authenticated-socket-queue/research.md, R6).
    /// </summary>
    /// <example>
    /// <code>
    /// IWebSocket socket = sockets.Create();
    /// socket.Open(url);
    /// </code>
    /// </example>
    internal interface IWebSocketFactory
    {
        /// <summary>Uma instância nova e ociosa.</summary>
        /// <example><code>IWebSocket socket = sockets.Create();</code></example>
        IWebSocket Create();
    }
}
