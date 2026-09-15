#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// A partida exatamente como o servidor mandou por último. Cada frame aceito substitui a visão inteira, sem
    /// mescla; os avisos saem na ordem de <c>specs/004-match-session/contracts/match-state.md</c>. Os fatos são
    /// leituras prontas da visão para desenhar: nenhum decide se uma jogada é legal.
    /// </summary>
    /// <example>
    /// <code>
    /// match.Mirror.ViewReplaced.Subscribe(change => Redraw(change.Current));
    /// bool canClick = match.Mirror.IsMyPriority;
    /// </code>
    /// </example>
    public sealed class MatchMirror
    {
        private readonly IClientLog log;
        private bool endAnnounced;

        internal MatchMirror(IClientLog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log that records failing subscribers");
            ViewReplaced = new EventFeed<ViewReplaced>("view_replaced", log);
            EventReceived = new EventFeed<MatchEvent>("event_received", log);
            PhaseChanged = new EventFeed<PhaseChange>("phase_changed", log);
            PriorityChanged = new EventFeed<PriorityChange>("priority_changed", log);
            MatchEnded = new EventFeed<MatchEnding>("match_ended", log);
        }

        /// <summary>A visão atual; nula antes do primeiro frame aceito.</summary>
        /// <example><code>PlayerView? view = mirror.Current;</code></example>
        public PlayerView? Current { get; private set; }

        /// <summary>A versão da visão atual; nula antes do primeiro frame aceito.</summary>
        /// <example><code>long? version = mirror.Version;</code></example>
        public long? Version { get; private set; }

        /// <summary>O próprio jogador, lido de <c>view.you.profile.user_id</c>.</summary>
        /// <example><code>UserId? self = mirror.Self;</code></example>
        public UserId? Self { get; private set; }

        /// <summary>A fase atual; nula antes do primeiro frame.</summary>
        /// <example><code>bool mulligan = mirror.Phase == MatchPhase.Mulligan;</code></example>
        public MatchPhase? Phase => Current?.Phase;

        /// <summary>A prioridade é do próprio jogador; falso no mulligan.</summary>
        /// <example><code>endTurnButton.interactable = mirror.IsMyPriority;</code></example>
        public bool IsMyPriority => Current != null && Current.PriorityUser == Self;

        /// <summary>O próprio jogador tem o token de ataque; falso no mulligan.</summary>
        /// <example><code>attackIcon.enabled = mirror.AmTokenHolder;</code></example>
        public bool AmTokenHolder => Current != null && Current.TokenHolder == Self;

        /// <summary>Fase de mulligan e o próprio mulligan ainda não foi enviado.</summary>
        /// <example><code>mulliganPanel.SetActive(mirror.MyMulliganPending);</code></example>
        public bool MyMulliganPending => Current != null && Current.Phase == MatchPhase.Mulligan && !Current.You.MulliganTaken;

        /// <summary>O oponente já respondeu o mulligan.</summary>
        /// <example><code>waitingLabel.enabled = !mirror.OpponentMulliganAnswered;</code></example>
        public bool OpponentMulliganAnswered => Current != null && Current.Opponent.MulliganTaken;

        /// <summary>A fase atual é <c>finished</c>.</summary>
        /// <example><code>bool over = mirror.IsFinished;</code></example>
        public bool IsFinished => Current != null && Current.Phase == MatchPhase.Finished;

        /// <summary>Venceu: nulo sem desfecho; verdadeiro quando o derrotado é o outro.</summary>
        /// <example><code>bool? won = mirror.DidIWin;</code></example>
        public bool? DidIWin => Current?.Outcome == null ? (bool?)null : Current.Outcome.DefeatedUser != Self;

        /// <summary>Estado substituído, antes de qualquer outro aviso do frame.</summary>
        /// <example><code>subscriptions.Add(mirror.ViewReplaced.Subscribe(change => Redraw(change.Current)));</code></example>
        public EventFeed<ViewReplaced> ViewReplaced { get; }

        /// <summary>Cada evento do frame, na ordem recebida.</summary>
        /// <example><code>subscriptions.Add(mirror.EventReceived.Subscribe(matchEvent => Animate(matchEvent)));</code></example>
        public EventFeed<MatchEvent> EventReceived { get; }

        /// <summary>A fase mudou em relação ao frame anterior.</summary>
        /// <example><code>subscriptions.Add(mirror.PhaseChanged.Subscribe(change => ShowPhase(change.Current)));</code></example>
        public EventFeed<PhaseChange> PhaseChanged { get; }

        /// <summary>A prioridade mudou em relação ao frame anterior.</summary>
        /// <example><code>subscriptions.Add(mirror.PriorityChanged.Subscribe(change => ShowPriority(change.Current)));</code></example>
        public EventFeed<PriorityChange> PriorityChanged { get; }

        /// <summary>A partida terminou; uma vez.</summary>
        /// <example><code>subscriptions.Add(mirror.MatchEnded.Subscribe(ending => ShowResult(ending.Won)));</code></example>
        public EventFeed<MatchEnding> MatchEnded { get; }

        internal void Apply(PlayerView view, long version, IReadOnlyList<MatchEvent> events)
        {
            PlayerView? previous = Current;
            // Um assinante quebrado não cala os avisos seguintes, porque cada feed isola o ouvinte; o estado já foi
            // substituído antes (FR-015).
            Replace(view, version);
            ViewReplaced.Publish(new ViewReplaced(previous, view));
            foreach (MatchEvent item in events)
                EventReceived.Publish(item);

            AnnounceChanges(previous, view);
            AnnounceEndOnce(view);
        }

        private void Replace(PlayerView view, long version)
        {
            UserId self = view.You.Profile.User;
            if (Self.HasValue && Self.Value != self)
                log.Error("match_self_changed", new LogField("previous", Self.Value.ToString()), new LogField("current", self.ToString()));

            Current = view;
            Version = version;
            Self = self;
        }

        private void AnnounceChanges(PlayerView? previous, PlayerView current)
        {
            if (previous == null)
                return;

            if (previous.Phase != current.Phase)
                PhaseChanged.Publish(new PhaseChange(previous.Phase, current.Phase));

            if (previous.PriorityUser != current.PriorityUser)
                PriorityChanged.Publish(new PriorityChange(previous.PriorityUser, current.PriorityUser));
        }

        private void AnnounceEndOnce(PlayerView view)
        {
            if (endAnnounced || view.Phase != MatchPhase.Finished || view.Outcome == null)
                return;

            endAnnounced = true;
            MatchOutcome outcome = view.Outcome;
            MatchEnded.Publish(new MatchEnding(outcome, outcome.DefeatedUser != Self));
        }
    }
}
