#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A saúde da conexão que sustenta o estado atual, para o overlay de reconexão: a de fila enquanto procura, a de
    /// partida do pareamento em diante; nos outros estágios não há conexão ativa e nada sai
    /// (specs/005-presentation-facade/contracts/client-state.md, "Saúde da conexão").
    /// </summary>
    /// <example>
    /// <code>
    /// subscriptions.Add(client.Health.Reconnecting.Subscribe(notice => ShowRetrying(notice.Attempt)));
    /// subscriptions.Add(client.Health.Recovered.Subscribe(_ => Hide()));
    /// </code>
    /// </example>
    public sealed class ConnectionHealth
    {
        private readonly Func<TimeSpan?> lastLatency;

        internal ConnectionHealth(IClientLog log, Func<TimeSpan?> lastLatency)
        {
            this.lastLatency = lastLatency ?? throw new ArgumentNullException(nameof(lastLatency), "last latency reader is null: expected a reader of the active connection");
            Reconnecting = new EventFeed<ReconnectingNotice>("health_reconnecting", log);
            Recovered = new EventFeed<RecoveredNotice>("health_recovered", log);
            GaveUp = new EventFeed<GaveUpNotice>("health_gave_up", log);
            LatencyMeasured = new EventFeed<TimeSpan>("health_latency", log);
        }

        /// <summary>A última latência medida pela conexão ativa; nula sem conexão ativa ou sem medida.</summary>
        /// <example><code>pingLabel.text = client.Health.LastLatency?.TotalMilliseconds.ToString("0") ?? "-";</code></example>
        public TimeSpan? LastLatency => lastLatency();

        /// <summary>Caiu e vai tentar de novo.</summary>
        /// <example><code>subscriptions.Add(client.Health.Reconnecting.Subscribe(notice => ShowRetrying(notice.Attempt)));</code></example>
        public EventFeed<ReconnectingNotice> Reconnecting { get; }

        /// <summary>Voltou depois de ter caído.</summary>
        /// <example><code>subscriptions.Add(client.Health.Recovered.Subscribe(_ => Hide()));</code></example>
        public EventFeed<RecoveredNotice> Recovered { get; }

        /// <summary>Não vai mais tentar.</summary>
        /// <example><code>subscriptions.Add(client.Health.GaveUp.Subscribe(notice => ShowFailed(notice.PlayerText)));</code></example>
        public EventFeed<GaveUpNotice> GaveUp { get; }

        /// <summary>Latência medida pelo heartbeat da conexão ativa.</summary>
        /// <example><code>subscriptions.Add(client.Health.LatencyMeasured.Subscribe(latency => ShowPing(latency)));</code></example>
        public EventFeed<TimeSpan> LatencyMeasured { get; }
    }
}
