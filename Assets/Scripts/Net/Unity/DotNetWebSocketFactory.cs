#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>Cria um <see cref="DotNetWebSocket"/> novo por tentativa, com a fila, a política de TLS e o log da camada.</summary>
    /// <example>
    /// <code>
    /// IWebSocketFactory sockets = new DotNetWebSocketFactory(queue, policy, log);
    /// IWebSocket socket = sockets.Create();
    /// </code>
    /// </example>
    public sealed class DotNetWebSocketFactory : IWebSocketFactory
    {
        private readonly MainThreadQueue queue;
        private readonly CleartextPolicy policy;
        private readonly IClientLog log;

        /// <summary>Fábrica com as dependências de todo socket real.</summary>
        /// <example><code>IWebSocketFactory sockets = new DotNetWebSocketFactory(queue, new CleartextPolicy(Debug.isDebugBuild), log);</code></example>
        public DotNetWebSocketFactory(MainThreadQueue queue, CleartextPolicy policy, IClientLog log)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy), "policy is null: expected the cleartext policy of this build");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        }

        /// <inheritdoc />
        public IWebSocket Create() => new DotNetWebSocket(queue, policy, log);
    }
}
