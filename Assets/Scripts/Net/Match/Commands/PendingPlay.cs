#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O comando que ainda não teve resposta, só para a apresentação evitar clique duplo: não bloqueia envio
    /// nenhum. Guarda à parte o último comando enviado desde a última atualização aceita, que a recusa usa por
    /// melhor esforço, porque o servidor não ecoa a mensagem recusada (specs/004-match-session/research.md, R7).
    /// </summary>
    /// <example>
    /// <code>
    /// subscriptions.Add(match.Pending.CurrentChanged.Subscribe(pending => spinner.SetActive(pending != null)));
    /// </code>
    /// </example>
    public sealed class PendingPlay
    {
        private PlayCommand? lastSentBefore;

        internal PendingPlay(IClientLog log)
        {
            CurrentChanged = new EventFeed<PlayCommand?>("pending_changed", log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log that records failing listeners"));
        }

        /// <summary>O comando pendente; nulo depois de atualização aceita, recusa ou reconexão.</summary>
        /// <example><code>bool waiting = pending.Current != null;</code></example>
        public PlayCommand? Current { get; private set; }

        /// <summary>O último comando enviado desde a última atualização aceita.</summary>
        /// <example><code>string last = pending.LastSentSinceUpdate?.MessageType ?? "none";</code></example>
        public PlayCommand? LastSentSinceUpdate { get; private set; }

        /// <summary>O pendente mudou.</summary>
        /// <example><code>subscriptions.Add(pending.CurrentChanged.Subscribe(command => spinner.SetActive(command != null)));</code></example>
        public EventFeed<PlayCommand?> CurrentChanged { get; }

        internal void MarkSent(PlayCommand command)
        {
            lastSentBefore = LastSentSinceUpdate;
            LastSentSinceUpdate = command;
            SetCurrent(command);
        }

        internal void Unmark(PlayCommand command)
        {
            if (ReferenceEquals(LastSentSinceUpdate, command))
                LastSentSinceUpdate = lastSentBefore;

            if (ReferenceEquals(Current, command))
                SetCurrent(null);
        }

        internal void ClearCurrent() => SetCurrent(null);

        internal void ClearOnUpdate()
        {
            lastSentBefore = null;
            LastSentSinceUpdate = null;
            SetCurrent(null);
        }

        private void SetCurrent(PlayCommand? next)
        {
            if (ReferenceEquals(Current, next))
                return;

            Current = next;
            CurrentChanged.Publish(next);
        }
    }
}
