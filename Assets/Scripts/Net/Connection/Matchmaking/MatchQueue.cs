#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// A fila sobre a conexão de fila: entra com um deck, reentra sozinha com o mesmo deck quando o
    /// socket cai procurando, entrega o pareamento e as recusas tipadas, e sai com motivo quando a
    /// conexão desiste. Contrato: specs/003-authenticated-socket-queue/contracts/matchmaking-queue.md.
    /// </summary>
    /// <example>
    /// <code>
    /// MatchQueue queue = new MatchQueue(connection, ConnectionTarget.Matchmaking(routes.Matchmaking), log);
    /// queue.Paired += pairing => match.Connect(pairing.Match);
    /// queue.Join(deck);
    /// </code>
    /// </example>
    internal sealed class MatchQueue : IDisposable
    {
        private readonly AuthenticatedConnection connection;
        private readonly ConnectionTarget target;
        private readonly IClientLog log;
        private readonly QueueRefusalReader refusals;

        /// <summary>Fila parada sobre a conexão dada.</summary>
        /// <example><code>MatchQueue queue = new MatchQueue(connection, target, log);</code></example>
        public MatchQueue(AuthenticatedConnection connection, ConnectionTarget target, IClientLog log)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection), "connection is null: expected the matchmaking AuthenticatedConnection");
            this.target = target ?? throw new ArgumentNullException(nameof(target), "target is null: expected ConnectionTarget.Matchmaking(routes.Matchmaking)");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
            refusals = new QueueRefusalReader(log);
            connection.StatusChanged += OnConnectionStatus;
            connection.FrameReceived += OnFrame;
        }

        /// <summary>Fase atual.</summary>
        /// <example><code>QueuePhase phase = queue.Phase;</code></example>
        public QueuePhase Phase { get; private set; } = QueuePhase.OutOfQueue;

        /// <summary>O deck da busca atual; nulo fora de busca.</summary>
        /// <example><code>DeckId? deck = queue.SearchDeck;</code></example>
        public DeckId? SearchDeck { get; private set; }

        /// <summary>A fase mudou.</summary>
        public event Action<QueuePhase>? PhaseChanged;

        /// <summary>O servidor pareou; o socket de fila já foi fechado.</summary>
        public event Action<MatchPairing>? Paired;

        /// <summary>O servidor recusou a entrada; o socket continua aberto.</summary>
        public event Action<QueueRefusal>? Refused;

        /// <summary>O pareamento falhou no servidor (<c>matchmaking_failed</c>), com o texto dele.</summary>
        public event Action<string>? MatchmakingFailed;

        /// <summary>A conexão desistiu durante a busca, com o motivo (FR-035).</summary>
        public event Action<GiveUpReason>? LeftQueue;

        /// <summary>Entra na fila com o deck. Conectando ou procurando, recusa localmente sem mandar nada (FR-028).</summary>
        /// <example><code>JoinOutcome outcome = queue.Join(new DeckId(4));</code></example>
        public JoinOutcome Join(DeckId deck)
        {
            if (IsSearching)
                return JoinOutcome.AlreadyQueued;

            SearchDeck = deck;
            SetPhase(QueuePhase.Connecting);
            if (connection.Status.Phase == ConnectionPhase.Connected)
                _ = SendJoinAsync(deck);
            else
                connection.Connect(target);

            return JoinOutcome.Started;
        }

        /// <summary>Sai da fila fechando o socket de propósito; nenhum aviso de falha (FR-029).</summary>
        /// <example><code>queue.Leave();</code></example>
        public void Leave()
        {
            SearchDeck = null;
            connection.Leave();
            SetPhase(QueuePhase.OutOfQueue);
        }

        /// <summary>Para de ouvir a conexão.</summary>
        /// <example><code>queue.Dispose();</code></example>
        public void Dispose()
        {
            connection.StatusChanged -= OnConnectionStatus;
            connection.FrameReceived -= OnFrame;
        }

        private bool IsSearching => Phase == QueuePhase.Connecting || Phase == QueuePhase.Searching;

        private void OnConnectionStatus(ConnectionStatus status)
        {
            if (status.Phase == ConnectionPhase.Connected && SearchDeck.HasValue)
                _ = SendJoinAsync(SearchDeck.Value);
            else if (status.Phase == ConnectionPhase.WaitingRetry || status.Phase == ConnectionPhase.RenewingToken || status.Phase == ConnectionPhase.Suspended)
                OnInterrupted();
            else if (status.Phase == ConnectionPhase.GaveUp)
                OnConnectionGaveUp(status.GiveUp!);
        }

        private async Task SendJoinAsync(DeckId deck)
        {
            // Cada abertura manda de novo: o servidor tirou a entrada quando o socket velho fechou, e um
            // join_queue repetido só substitui a entrada (contrato 011).
            SocketSendOutcome sent = await connection.SendAsync(new JoinQueueMessage(deck));
            if (sent.Status != SocketSendStatus.Sent || Phase != QueuePhase.Connecting || !deck.Equals(SearchDeck))
                return;

            log.Info("queue_join_sent", new LogField("deck_id", deck.Value));
            SetPhase(QueuePhase.Searching);
        }

        private void OnInterrupted()
        {
            if (SearchDeck.HasValue)
            {
                SetPhase(QueuePhase.Connecting);
                return;
            }

            // Socket de fila sem busca não sustenta nada: reconectá-lo gastaria tentativas à toa (FR-036).
            connection.Leave();
        }

        private void OnConnectionGaveUp(GiveUpReason reason)
        {
            if (!SearchDeck.HasValue)
                return;

            SearchDeck = null;
            SetPhase(QueuePhase.OutOfQueue);
            log.Warning("queue_left", new LogField("kind", reason.Kind.ToString()));
            LeftQueue?.Invoke(reason);
        }

        private void OnFrame(ServerFrame frame)
        {
            if (frame is MatchFoundFrame found)
                OnMatchFound(found.Pairing);
            else if (frame is MessageRefusedFrame refused)
                OnRefused(refusals.Read(refused));
            else if (frame is MatchmakingFailedFrame failed)
                OnMatchmakingFailed(failed.Error);
        }

        private void OnMatchFound(MatchPairing pairing)
        {
            if (!IsSearching)
                return;

            SearchDeck = null;
            connection.Leave();
            SetPhase(QueuePhase.Paired);
            Paired?.Invoke(pairing);
        }

        private void OnRefused(QueueRefusal refusal)
        {
            log.Info("queue_refused", new LogField("code", refusal.Code));
            if (!IsSearching)
                return;

            SearchDeck = null;
            SetPhase(QueuePhase.OutOfQueue);
            Refused?.Invoke(refusal);
        }

        private void OnMatchmakingFailed(string error)
        {
            if (!IsSearching)
                return;

            SearchDeck = null;
            SetPhase(QueuePhase.OutOfQueue);
            MatchmakingFailed?.Invoke(error);
        }

        private void SetPhase(QueuePhase next)
        {
            if (next == Phase)
                return;

            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
