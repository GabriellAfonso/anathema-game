#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Traduz o estado das duas conexões para a saúde da conexão ativa. A tradução veio do antigo <c>BaseClient</c>,
    /// que o overlay assinava por cliente de socket (specs/005-presentation-facade/research.md, R10).
    /// </summary>
    internal sealed class HealthRelay : IDisposable
    {
        private readonly ConnectionServices connections;
        private readonly ClientStages stages;
        private readonly List<Action> detach = new List<Action>();

        // Numero da ultima tentativa anunciada: a suspensao sem rede nao carrega tentativa propria, e o
        // aviso continua mostrando a da queda que a causou.
        private int lastAttempt = 1;

        internal HealthRelay(ConnectionServices connections, ClientStages stages, IClientLog log)
        {
            this.connections = connections;
            this.stages = stages;
            Health = new ConnectionHealth(log, () => Active?.LastLatency);
            Listen(connections.MatchmakingConnection);
            Listen(connections.MatchConnection);
        }

        internal ConnectionHealth Health { get; }

        private AuthenticatedConnection? Active => stages.State.Stage switch
        {
            ClientStage.Searching => connections.MatchmakingConnection,
            ClientStage.Paired or ClientStage.InMatch or ClientStage.MatchFinished or ClientStage.MatchUnavailable => connections.MatchConnection,
            _ => null,
        };

        public void Dispose()
        {
            foreach (Action undo in detach)
                undo();

            detach.Clear();
        }

        private void Listen(AuthenticatedConnection source)
        {
            Action<ConnectionStatus> status = value => OnStatus(source, value);
            Action recovered = () => OnRecovered(source);
            Action<TimeSpan> latency = value => OnLatency(source, value);
            source.StatusChanged += status;
            source.Recovered += recovered;
            source.LatencyMeasured += latency;
            detach.Add(() => { source.StatusChanged -= status; source.Recovered -= recovered; source.LatencyMeasured -= latency; });
        }

        private void OnStatus(AuthenticatedConnection source, ConnectionStatus status)
        {
            if (!ReferenceEquals(source, Active))
                return;

            if (status.Phase == ConnectionPhase.WaitingRetry)
                AnnounceRetry(status.Attempt, status.Wait);
            else if (status.Phase == ConnectionPhase.Suspended && status.Suspension == SuspensionReason.NoNetwork)
                AnnounceRetry(lastAttempt, TimeSpan.Zero);
            else if (status.Phase == ConnectionPhase.GaveUp)
                Health.GaveUp.Publish(new GaveUpNotice(status.GiveUp!));
        }

        private void AnnounceRetry(int attempt, TimeSpan wait)
        {
            lastAttempt = attempt;
            Health.Reconnecting.Publish(new ReconnectingNotice(attempt, wait));
        }

        private void OnRecovered(AuthenticatedConnection source)
        {
            if (ReferenceEquals(source, Active))
                Health.Recovered.Publish(new RecoveredNotice());
        }

        private void OnLatency(AuthenticatedConnection source, TimeSpan latency)
        {
            if (ReferenceEquals(source, Active))
                Health.LatencyMeasured.Publish(latency);
        }
    }
}
