#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O relógio da vez e do mulligan, desenhável a qualquer momento. Conta a partir do <c>remaining_ms</c> medido
    /// pelo servidor e do instante monotônico em que o frame chegou, nunca por hora absoluta
    /// (<c>backend/specs/010-match-timers/contracts/server_frames.md</c>). Não tem temporizador: chegar a zero não
    /// faz nada, porque quem estoura a vez é o servidor.
    /// </summary>
    /// <example>
    /// <code>
    /// TimeSpan? left = match.Clock.TurnRemaining;
    /// timerLabel.text = left.HasValue ? left.Value.TotalSeconds.ToString("0") : string.Empty;
    /// </code>
    /// </example>
    public sealed class TurnClock
    {
        private readonly IMonotonicClock clock;
        private readonly IClientLog log;
        private MonotonicInstant turnAnchor;
        private MonotonicInstant mulliganAnchor;
        private long? mulliganMs;
        private long? warnedTurn;

        internal TurnClock(IMonotonicClock clock, IClientLog log)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "clock is null: expected the monotonic clock of the device");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
            TurnStarted = new EventFeed<TurnView>("turn_started", log);
            TurnRunningOut = new EventFeed<long>("turn_running_out", log);
        }

        /// <summary>A vez desenhada, com o restante da última âncora; nula sem vez.</summary>
        /// <example><code>UserId? holder = clock.Turn?.Holder;</code></example>
        public TurnView? Turn { get; private set; }

        /// <summary>Quanto falta da vez agora, nunca negativo; nulo sem vez.</summary>
        /// <example><code>TimeSpan? left = clock.TurnRemaining;</code></example>
        public TimeSpan? TurnRemaining => Turn == null ? (TimeSpan?)null : RemainingSince(Turn.RemainingMs, turnAnchor);

        /// <summary>Quanto falta do próprio mulligan agora, nunca negativo; nulo fora do mulligan.</summary>
        /// <example><code>TimeSpan? left = clock.MulliganRemaining;</code></example>
        public TimeSpan? MulliganRemaining => mulliganMs.HasValue ? RemainingSince(mulliganMs.Value, mulliganAnchor) : (TimeSpan?)null;

        /// <summary>Começou uma vez nova (<c>turn_number</c> diferente do desenhado).</summary>
        /// <example><code>subscriptions.Add(clock.TurnStarted.Subscribe(turn => ResetTimerBar(turn)));</code></example>
        public EventFeed<TurnView> TurnStarted { get; }

        /// <summary>O tempo da vez está acabando; no máximo uma vez por <c>turn_number</c>.</summary>
        /// <example><code>subscriptions.Add(clock.TurnRunningOut.Subscribe(turnNumber => FlashTimer()));</code></example>
        public EventFeed<long> TurnRunningOut { get; }

        internal ClockAnnouncements Anchor(ClockView view, MonotonicInstant arrival)
        {
            ClockAnnouncements pending = new ClockAnnouncements(this);
            AnchorTurn(view.Turn, arrival, pending);
            mulliganMs = view.MulliganRemainingMs;
            mulliganAnchor = arrival;
            return pending;
        }

        internal void NoteWarning(TurnWarningFrame warning, MonotonicInstant arrival)
        {
            if (Turn == null || Turn.TurnNumber != warning.TurnNumber)
            {
                // Aviso de outra vez, ou que chegou antes do frame da vez que cita (contrato 010).
                log.Debug("turn_warning_ignored", new LogField("turn_number", warning.TurnNumber), new LogField("drawn_turn_number", Turn == null ? "none" : Turn.TurnNumber.ToString()));
                return;
            }

            Turn = new TurnView(warning.TurnNumber, Turn.Holder, warning.RemainingMs, true);
            turnAnchor = arrival;
            if (MarkWarned(warning.TurnNumber))
                RaiseRunningOut(warning.TurnNumber);
        }

        internal void RaiseStarted(TurnView turn) => TurnStarted.Publish(turn);

        internal void RaiseRunningOut(long turnNumber) => TurnRunningOut.Publish(turnNumber);

        private void AnchorTurn(TurnView? next, MonotonicInstant arrival, ClockAnnouncements pending)
        {
            bool isNew = next != null && (Turn == null || Turn.TurnNumber != next.TurnNumber);
            Turn = next;
            turnAnchor = arrival;
            if (next == null)
                return;

            if (isNew)
                pending.MarkStarted(next);

            if (next.Warning && MarkWarned(next.TurnNumber))
                pending.MarkRunningOut(next.TurnNumber);
        }

        private bool MarkWarned(long turnNumber)
        {
            if (warnedTurn == turnNumber)
                return false;

            warnedTurn = turnNumber;
            return true;
        }

        private TimeSpan RemainingSince(long remainingMs, MonotonicInstant anchor)
        {
            TimeSpan left = TimeSpan.FromMilliseconds(remainingMs) - (clock.Now - anchor);
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }
    }
}
