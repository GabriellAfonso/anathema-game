#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Json;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Os adaptadores reais montados juntos, com a mesma fila, o mesmo log e a mesma política
    /// de cleartext. Existe para que os testes LiveServer e o probe do aparelho componham a
    /// camada do mesmo jeito.
    /// </summary>
    /// <example>
    /// <code>
    /// LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(Debug.isDebugBuild));
    /// IWebSocket socket = adapters.CreateSocket();
    /// </code>
    /// </example>
    public sealed class LiveNetworkAdapters
    {
        private LiveNetworkAdapters(MainThreadQueue queue, IClientLog log, CleartextPolicy policy)
        {
            Queue = queue;
            Log = log;
            Policy = policy;
            Http = new UnityHttpTransport(queue, policy);
            Clock = PlatformMonotonicClock.Create(log);
            Sockets = new DotNetWebSocketFactory(queue, policy, log);
            // Um codec só para a conta e os dois sockets: os frames de fila entram na mesma união
            // (specs/003-authenticated-socket-queue/research.md, R1).
            Codec = new NewtonsoftProtocolCodec(ConnectionFrames.CreateUnion(), log);
        }

        /// <summary>Fila da thread principal compartilhada.</summary>
        /// <example><code>adapters.Queue.Drain();</code></example>
        public MainThreadQueue Queue { get; }

        /// <summary>Log compartilhado.</summary>
        /// <example><code>adapters.Log.Info("probe_started");</code></example>
        public IClientLog Log { get; }

        /// <summary>Política de cleartext entregue ao socket e ao HTTP.</summary>
        /// <example><code>bool development = adapters.Policy.AllowsCleartext;</code></example>
        public CleartextPolicy Policy { get; }

        /// <summary>Transporte HTTP real.</summary>
        /// <example><code>HttpOutcome outcome = await adapters.Http.SendAsync(request);</code></example>
        public IHttpTransport Http { get; }

        /// <summary>Relógio monotônico do alvo.</summary>
        /// <example><code>MonotonicInstant now = adapters.Clock.Now;</code></example>
        public IMonotonicClock Clock { get; }

        /// <summary>Codec com os frames genéricos.</summary>
        /// <example><code>string ping = adapters.Codec.Encode(new PingMessage(null));</code></example>
        public IProtocolCodec Codec { get; }

        /// <summary>Um DotNetWebSocket novo por tentativa de conexão.</summary>
        /// <example><code>IWebSocket socket = adapters.Sockets.Create();</code></example>
        public IWebSocketFactory Sockets { get; }

        /// <summary>Monta os adaptadores.</summary>
        /// <example><code>LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, policy);</code></example>
        public static LiveNetworkAdapters Create(MainThreadQueue queue, IClientLog log, CleartextPolicy policy)
        {
            if (queue == null || log == null || policy == null)
                throw new ArgumentNullException(queue == null ? nameof(queue) : log == null ? nameof(log) : nameof(policy), "live adapters need a queue, a log and a cleartext policy: expected all three");

            return new LiveNetworkAdapters(queue, log, policy);
        }

        /// <summary>Uma conexão de socket nova (uma instância por conexão).</summary>
        /// <example><code>IWebSocket socket = adapters.CreateSocket();</code></example>
        public IWebSocket CreateSocket() => Sockets.Create();
    }
}
