#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A fila pela fachada: entrar com um deck, sair, e os avisos de pareado, recusa tipada e saída da fila. Recusa
    /// e saída levam o app de volta a <see cref="ClientStage.SignedIn"/>, em vez de só irem para o log
    /// (specs/005-presentation-facade/contracts/client-state.md, "Fila").
    /// </summary>
    /// <example>
    /// <code>
    /// subscriptions.Add(client.Queue.Refused.Subscribe(refusal => ShowRefusal(refusal.Kind)));
    /// QueueJoinResult result = client.Queue.Join(deck.Deck);
    /// </code>
    /// </example>
    public sealed class ClientQueue
    {
        private readonly MatchQueue queue;
        private readonly ClientStages stages;
        private readonly Action<MatchPairing> openMatch;

        internal ClientQueue(MatchQueue queue, ClientStages stages, IClientLog log, Action<MatchPairing> openMatch)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the MatchQueue over the matchmaking connection");
            this.stages = stages ?? throw new ArgumentNullException(nameof(stages), "stages are null: expected the client stages owner");
            this.openMatch = openMatch ?? throw new ArgumentNullException(nameof(openMatch), "open match is null: expected the action that opens the paired match");
            Paired = new EventFeed<MatchPairing>("queue_paired", log);
            Refused = new EventFeed<QueueRefusal>("queue_refused", log);
            Left = new EventFeed<QueueExit>("queue_left", log);
            Subscribe(queue);
        }

        /// <summary>A fase da fila.</summary>
        /// <example><code>bool searching = client.Queue.Phase == QueuePhase.Searching;</code></example>
        public QueuePhase Phase => queue.Phase;

        /// <summary>Pareado, com a partida e os dois jogadores; sai depois de o app ir para <see cref="ClientStage.Paired"/>.</summary>
        /// <example><code>subscriptions.Add(client.Queue.Paired.Subscribe(pairing => ShowOpponent(pairing.Opponent)));</code></example>
        public EventFeed<MatchPairing> Paired { get; }

        /// <summary>O servidor recusou o pedido de fila; compare pelo <see cref="QueueRefusal.Kind"/>.</summary>
        /// <example><code>subscriptions.Add(client.Queue.Refused.Subscribe(refusal => ShowRefusal(refusal.Kind)));</code></example>
        public EventFeed<QueueRefusal> Refused { get; }

        /// <summary>A busca acabou sem pareamento e sem pedido do jogador.</summary>
        /// <example><code>subscriptions.Add(client.Queue.Left.Subscribe(exit => ShowExit(exit)));</code></example>
        public EventFeed<QueueExit> Left { get; }

        /// <summary>Entra na fila com o deck; só vale em <see cref="ClientStage.SignedIn"/>.</summary>
        /// <example><code>QueueJoinResult result = client.Queue.Join(deck.Deck);</code></example>
        public QueueJoinResult Join(DeckId deck)
        {
            ClientState state = stages.State;
            if (state.Stage == ClientStage.Searching)
                return QueueJoinResult.Of(QueueJoinKind.AlreadyQueued, state.Stage);

            if (state.Stage != ClientStage.SignedIn)
                return QueueJoinResult.Of(QueueJoinKind.NotApplicable, state.Stage);

            queue.Join(deck);
            stages.Move(ClientState.Searching(state.Self!.Value), "queue_joined");
            return QueueJoinResult.Of(QueueJoinKind.Started, ClientStage.Searching);
        }

        /// <summary>Sai da fila; só vale em <see cref="ClientStage.Searching"/>.</summary>
        /// <example><code>StageRequestResult result = client.Queue.Leave();</code></example>
        public StageRequestResult Leave()
        {
            ClientState state = stages.State;
            if (state.Stage != ClientStage.Searching)
                return StageRequestResult.NotApplicable(state.Stage);

            queue.Leave();
            stages.Move(ClientState.SignedIn(state.Self!.Value), "queue_left_by_player");
            return StageRequestResult.Done(ClientStage.SignedIn);
        }

        private void Subscribe(MatchQueue source)
        {
            source.Paired += OnPaired;
            source.Refused += OnRefused;
            source.LeftQueue += OnLeftQueue;
            source.MatchmakingFailed += OnMatchmakingFailed;
        }

        private void OnPaired(MatchPairing pairing)
        {
            ClientState state = stages.State;
            if (state.Stage != ClientStage.Searching)
                return;

            stages.Move(ClientState.Paired(state.Self!.Value, pairing), "match_found");
            openMatch(pairing);
            Paired.Publish(pairing);
        }

        private void OnRefused(QueueRefusal refusal)
        {
            if (BackToSignedIn("queue_refused"))
                Refused.Publish(refusal);
        }

        private void OnLeftQueue(GiveUpReason reason)
        {
            // Desistência por sessão perdida é da SessionExpiryWatch: leva ao login, não de volta ao início.
            if (!SessionExpiryWatch.IsSessionLoss(reason) && BackToSignedIn("queue_connection_gave_up"))
                Left.Publish(QueueExit.ConnectionGaveUp(reason));
        }

        private void OnMatchmakingFailed(string error)
        {
            if (BackToSignedIn("matchmaking_failed"))
                Left.Publish(QueueExit.MatchmakingFailed(error));
        }

        private bool BackToSignedIn(string cause)
        {
            ClientState state = stages.State;
            if (state.Stage != ClientStage.Searching)
                return false;

            stages.Move(ClientState.SignedIn(state.Self!.Value), cause);
            return true;
        }
    }
}
