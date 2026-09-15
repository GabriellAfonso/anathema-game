#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// O que uma conexão recebe do mundo de fora: fábrica de socket, relógio, quadros, ciclo de vida,
    /// rede, fila da thread principal e log. Agrupado para o construtor da conexão não passar de
    /// poucos parâmetros; as mesmas portas servem às duas conexões.
    /// </summary>
    /// <example>
    /// <code>
    /// ConnectionPorts ports = new ConnectionPorts(sockets, clock, ticker, lifecycle, reachability, queue, log);
    /// </code>
    /// </example>
    internal sealed class ConnectionPorts
    {
        /// <summary>Portas validadas; nenhuma pode ser nula.</summary>
        /// <example><code>ConnectionPorts ports = new ConnectionPorts(sockets, clock, ticker, lifecycle, reachability, queue, log);</code></example>
        public ConnectionPorts(IWebSocketFactory sockets, IMonotonicClock clock, IFrameTicker ticker, IAppLifecycle lifecycle,
            INetworkReachability reachability, MainThreadQueue queue, IClientLog log)
        {
            Sockets = sockets ?? throw Missing(nameof(sockets), "the factory that creates one socket per attempt");
            Clock = clock ?? throw Missing(nameof(clock), "the monotonic clock");
            Ticker = ticker ?? throw Missing(nameof(ticker), "the frame ticker that drives waits and silence");
            Lifecycle = lifecycle ?? throw Missing(nameof(lifecycle), "the app lifecycle");
            Reachability = reachability ?? throw Missing(nameof(reachability), "the network reachability");
            Queue = queue ?? throw Missing(nameof(queue), "the main thread queue");
            Log = log ?? throw Missing(nameof(log), "the client log");
        }

        /// <summary>Uma instância de socket por tentativa.</summary>
        /// <example><code>IWebSocket socket = ports.Sockets.Create();</code></example>
        public IWebSocketFactory Sockets { get; }

        /// <summary>Relógio monotônico do aparelho.</summary>
        /// <example><code>MonotonicInstant now = ports.Clock.Now;</code></example>
        public IMonotonicClock Clock { get; }

        /// <summary>Um aviso por quadro.</summary>
        /// <example><code>ports.Ticker.Ticked += OnTick;</code></example>
        public IFrameTicker Ticker { get; }

        /// <summary>Ida e volta do segundo plano.</summary>
        /// <example><code>ports.Lifecycle.ReturnedToForeground += OnForeground;</code></example>
        public IAppLifecycle Lifecycle { get; }

        /// <summary>Tipo de rede e mudanças.</summary>
        /// <example><code>NetworkKind kind = ports.Reachability.Current;</code></example>
        public INetworkReachability Reachability { get; }

        /// <summary>Fila da thread principal, usada para confirmar silêncio atrás dos frames já recebidos.</summary>
        /// <example><code>ports.Queue.Enqueue(ConfirmSilence);</code></example>
        public MainThreadQueue Queue { get; }

        /// <summary>Log estruturado.</summary>
        /// <example><code>ports.Log.Info("connection_opening");</code></example>
        public IClientLog Log { get; }

        private static ArgumentNullException Missing(string name, string expected)
        {
            return new ArgumentNullException(name, $"{name} is null: expected {expected}");
        }
    }
}
