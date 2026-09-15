#nullable enable
using System;
using Anathema.Net.Connection;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A busca acabou sem pareamento e sem o jogador pedir: a conexão de fila desistiu, ou o servidor avisou
    /// <c>matchmaking_failed</c>. O app volta a <see cref="ClientStage.SignedIn"/> (US4-3).
    /// </summary>
    /// <example>
    /// <code>
    /// subscriptions.Add(client.Queue.Left.Subscribe(exit => banner.text = exit.GiveUp?.PlayerText() ?? "A busca foi interrompida."));
    /// </code>
    /// </example>
    public sealed class QueueExit
    {
        private QueueExit(QueueExitKind kind, GiveUpReason? giveUp, string errorForLog)
        {
            Kind = kind;
            GiveUp = giveUp;
            ErrorForLog = errorForLog;
        }

        /// <summary>Por quê.</summary>
        /// <example><code>QueueExitKind kind = exit.Kind;</code></example>
        public QueueExitKind Kind { get; }

        /// <summary>O motivo da conexão, em <see cref="QueueExitKind.ConnectionGaveUp"/>.</summary>
        /// <example><code>GiveUpKind? kind = exit.GiveUp?.Kind;</code></example>
        public GiveUpReason? GiveUp { get; }

        /// <summary>O <c>error</c> do servidor em <see cref="QueueExitKind.MatchmakingFailed"/>; só para log, nunca comparado.</summary>
        /// <example><code>log.Warning("queue_exit", new LogField("error", exit.ErrorForLog));</code></example>
        public string ErrorForLog { get; }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = exit.ToString(); // ConnectionGaveUp kind=AttemptsExhausted ...</code></example>
        public override string ToString() => GiveUp == null ? $"{Kind} error={ErrorForLog}" : $"{Kind} {GiveUp}";

        internal static QueueExit ConnectionGaveUp(GiveUpReason reason)
        {
            GiveUpReason required = reason ?? throw new ArgumentNullException(nameof(reason), "queue give up reason is null: expected the reason from the matchmaking connection");
            return new QueueExit(QueueExitKind.ConnectionGaveUp, required, string.Empty);
        }

        internal static QueueExit MatchmakingFailed(string error) => new QueueExit(QueueExitKind.MatchmakingFailed, null, error ?? string.Empty);
    }
}
