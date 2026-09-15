#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// A sessão de uma partida: abre a conexão autenticada no alvo de partida e junta espelho, relógio, comandos e
    /// narrador. Atravessa quedas marcando o estado como desatualizado até o <c>match_start</c> da reconexão
    /// ressincronizar, e fecha o socket de propósito no fim, porque o servidor não fecha
    /// (specs/004-match-session/contracts/live-match.md, research R4, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// LiveMatch match = new LiveMatch(connection, routes.Match, pairing.Match, catalog, clock, log);
    /// match.Mirror.ViewReplaced.Subscribe(change => Redraw(change.Current));
    /// match.Start();
    /// </code>
    /// </example>
    public sealed class LiveMatch : IDisposable
    {
        private static readonly IReadOnlyList<MatchEvent> NoEvents = Array.Empty<MatchEvent>();

        private readonly AuthenticatedConnection connection;
        private readonly ConnectionTarget target;
        private readonly LoadedCatalog catalog;
        private readonly IMonotonicClock time;
        private readonly IClientLog log;
        private readonly VersionGate gate = new VersionGate();
        private readonly MatchNarrator narrator;
        private bool started;
        private bool disposed;

        /// <summary>Sessão pronta para <see cref="Start"/>, sobre a conexão de partida compartilhada.</summary>
        /// <example><code>LiveMatch match = new LiveMatch(connection, routes.Match, pairing.Match, catalog, clock, log);</code></example>
        internal LiveMatch(AuthenticatedConnection connection, Uri matchBase, MatchId match, LoadedCatalog catalog, IMonotonicClock clock, IClientLog log)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection), $"connection of {match} is null: expected the match AuthenticatedConnection");
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog), $"catalog of {match} is null: expected the loaded card catalog");
            time = clock ?? throw new ArgumentNullException(nameof(clock), $"clock of {match} is null: expected the monotonic clock");
            this.log = log ?? throw new ArgumentNullException(nameof(log), $"log of {match} is null: expected the client log");
            target = ConnectionTarget.Match(matchBase, match);
            Match = match;
            Pending = new PendingPlay(log);
            Mirror = new MatchMirror(log);
            Clock = new TurnClock(clock, log);
            Commands = new MatchCommands(connection, Pending, log);
            StatusChanged = new EventFeed<LiveMatchStatus>("match_status_changed", log);
            Refused = new EventFeed<PlayRefusal>("match_play_refused", log);
            narrator = new MatchNarrator(Mirror, catalog, match, log);
        }

        /// <summary>A partida desta sessão.</summary>
        /// <example><code>MatchId match = live.Match;</code></example>
        public MatchId Match { get; }

        /// <summary>O estado da sessão.</summary>
        /// <example><code>bool live = match.Status.Phase == LiveMatchPhase.Live;</code></example>
        public LiveMatchStatus Status { get; private set; } = LiveMatchStatus.Of(LiveMatchPhase.Idle);

        /// <summary>O estado espelhado.</summary>
        /// <example><code>PlayerView? view = match.Mirror.Current;</code></example>
        public MatchMirror Mirror { get; }

        /// <summary>O relógio da vez e do mulligan.</summary>
        /// <example><code>TimeSpan? left = match.Clock.TurnRemaining;</code></example>
        public TurnClock Clock { get; }

        /// <summary>As jogadas.</summary>
        /// <example><code>await match.Commands.Pass();</code></example>
        public MatchCommands Commands { get; }

        /// <summary>O comando sem resposta, para evitar clique duplo.</summary>
        /// <example><code>bool waiting = match.Pending.Current != null;</code></example>
        public PendingPlay Pending { get; }

        /// <summary>O estado da sessão mudou.</summary>
        /// <example><code>subscriptions.Add(match.StatusChanged.Subscribe(status => ShowStatus(status)));</code></example>
        public EventFeed<LiveMatchStatus> StatusChanged { get; }

        /// <summary>O servidor recusou uma jogada.</summary>
        /// <example><code>subscriptions.Add(match.Refused.Subscribe(refusal => ShowRefusal(refusal.Code)));</code></example>
        public EventFeed<PlayRefusal> Refused { get; }

        /// <summary>Assina a conexão e abre o socket de partida; uma vez por sessão.</summary>
        /// <example><code>match.Start();</code></example>
        public void Start()
        {
            if (started || disposed)
                throw new InvalidOperationException($"live match {Match} is {(disposed ? "disposed" : "already started")}: expected one Start per LiveMatch");

            started = true;
            connection.StatusChanged += OnConnectionStatus;
            connection.FrameReceived += OnFrame;
            connection.Recovered += OnRecovered;
            SetStatus(LiveMatchStatus.Of(LiveMatchPhase.Connecting));
            connection.Connect(target);
        }

        /// <summary>A dica de mira de uma cópia da mão atual; fora da mão antes do primeiro estado.</summary>
        /// <example><code>HandCardHint hint = match.HintFor(card.Instance);</code></example>
        public HandCardHint HintFor(CardInstanceId card)
        {
            PlayerView? view = Mirror.Current;
            return view == null ? HandCardHint.NotInHand(card, null) : HandCardHints.For(card, view, catalog, log);
        }

        /// <summary>Ataque e vida para exibir; só conveniência de exibição.</summary>
        /// <example><code>DisplayedUnitStats? stats = match.StatsOf(unit);</code></example>
        public DisplayedUnitStats? StatsOf(BankUnit unit) => DisplayedUnitStats.Of(unit, catalog);

        /// <summary>Sai da conexão de propósito e cala todos os avisos desta sessão.</summary>
        /// <example><code>match.Dispose();</code></example>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            narrator.Dispose();
            if (!started)
                return;

            connection.StatusChanged -= OnConnectionStatus;
            connection.FrameReceived -= OnFrame;
            connection.Recovered -= OnRecovered;
            connection.Leave();
        }

        private bool IsTerminal => Status.Phase == LiveMatchPhase.Finished || Status.Phase == LiveMatchPhase.Refused || Status.Phase == LiveMatchPhase.GaveUp;

        private void OnConnectionStatus(ConnectionStatus status)
        {
            if (IsTerminal)
                return;

            if (status.Phase == ConnectionPhase.GaveUp)
                OnConnectionGaveUp(status.GiveUp!);
            else if (IsInterruption(status.Phase) && Status.Phase == LiveMatchPhase.Live)
                SetStatus(LiveMatchStatus.Reconnecting());
        }

        private static bool IsInterruption(ConnectionPhase phase)
        {
            return phase == ConnectionPhase.Connecting || phase == ConnectionPhase.WaitingRetry || phase == ConnectionPhase.RenewingToken || phase == ConnectionPhase.Suspended;
        }

        private void OnConnectionGaveUp(GiveUpReason reason)
        {
            SetStatus(reason.Kind == GiveUpKind.MatchRefused ? LiveMatchStatus.RefusedBy(reason, Status.IsStale) : LiveMatchStatus.GivenUp(reason));
        }

        private void OnFrame(ServerFrame frame)
        {
            // Lido antes de qualquer trabalho: o relógio conta da entrega (specs/004-match-session/research.md, R5).
            MonotonicInstant arrival = time.Now;
            if (IsTerminal)
                return;

            if (frame is MatchStartFrame start)
                OnState(start.MessageType, start.Version, start.View, NoEvents, start.Clock, arrival);
            else if (frame is MatchUpdateFrame update)
                OnState(update.MessageType, update.Version, update.View, update.Events, update.Clock, arrival);
            else
                OnOtherFrame(frame, arrival);
        }

        private void OnOtherFrame(ServerFrame frame, MonotonicInstant arrival)
        {
            if (frame is TurnWarningFrame warning)
                Clock.NoteWarning(warning, arrival);
            else if (frame is MessageRefusedFrame refused)
                OnRefused(refused);
            else if (!(frame is PongFrame || frame is AuthDeniedFrame || frame is MatchDeniedFrame))
                log.Debug("match_frame_unexpected", new LogField("type", frame.MessageType));
        }

        private void OnState(string type, long version, PlayerView view, IReadOnlyList<MatchEvent> events, ClockView clock, MonotonicInstant arrival)
        {
            FrameVerdict verdict = gate.Judge(version, type == MatchStartFrame.TypeName);
            if (verdict == FrameVerdict.Discard)
            {
                long applied = gate.AppliedVersion ?? 0;
                log.Debug("match_frame_discarded", new LogField("type", type), new LogField("version", version), new LogField("applied_version", applied));
                return;
            }

            if (verdict == FrameVerdict.ResyncSameVersion)
                Resync(clock, arrival);
            else
                Accept(version, view, events, clock, arrival);
        }

        private void Resync(ClockView clock, MonotonicInstant arrival)
        {
            // Mesma versão depois de reconectar: o estado não muda, mas o relógio mediu de novo (FR-021).
            Clock.Anchor(clock, arrival).Raise();
            SettleAfterState();
        }

        private void Accept(long version, PlayerView view, IReadOnlyList<MatchEvent> events, ClockView clock, MonotonicInstant arrival)
        {
            gate.Record(version);
            ClockAnnouncements clockNotices = Clock.Anchor(clock, arrival);
            // Antes dos avisos: quem responde ao estado novo mandando um comando não pode tê-lo apagado logo depois.
            Pending.ClearOnUpdate();
            Mirror.Apply(view, version, events);
            // Assinante do relógio que lança vai para o log pelo próprio feed, sem calar o resto do frame (FR-015).
            clockNotices.Raise();
            SettleAfterState();
        }

        private void SettleAfterState()
        {
            if (Mirror.IsFinished)
            {
                // O servidor não fecha o socket de partida no fim: sair é do cliente (FR-034).
                SetStatus(LiveMatchStatus.FinishedWith(Mirror.Current!.Outcome));
                connection.Leave();
                return;
            }

            if (Status.Phase != LiveMatchPhase.Live)
                SetStatus(LiveMatchStatus.Of(LiveMatchPhase.Live));
        }

        private void OnRefused(MessageRefusedFrame refused)
        {
            // O servidor não ecoa a mensagem recusada: o comando associado é o último enviado, por melhor esforço (FR-026).
            PlayRefusal refusal = PlayRefusalReader.Read(refused, Pending.LastSentSinceUpdate);
            log.Warning("match_play_refused", new LogField("code", refusal.CodeText), new LogField("error", refusal.Error),
                new LogField("probable_command", refusal.ProbableCommand?.MessageType ?? "none"));
            Pending.ClearCurrent();
            Refused.Publish(refusal);
        }

        private void OnRecovered()
        {
            Pending.ClearCurrent();
        }

        private void SetStatus(LiveMatchStatus next)
        {
            if (next.Equals(Status))
                return;

            Status = next;
            log.Info("match_status", new LogField("match_id", Match.Value), new LogField("phase", next.Phase.ToString()), new LogField("stale", next.IsStale),
                new LogField("give_up_kind", next.GiveUp == null ? "none" : next.GiveUp.Kind.ToString()));
            StatusChanged.Publish(next);
        }
    }
}
