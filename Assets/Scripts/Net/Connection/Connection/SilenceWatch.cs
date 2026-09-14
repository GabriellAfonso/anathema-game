#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Heartbeat do socket aberto, movido pelos quadros. Duas proteções contra derrubar conexão boa
    /// (specs/003-authenticated-socket-queue/research.md, R3):
    /// <list type="bullet">
    /// <item>salto entre quadros acima do limiar de pausa zera o silêncio antes de avaliar;</item>
    /// <item>a morte só é confirmada atrás da <see cref="MainThreadQueue"/>, depois dos frames que o
    /// socket já recebeu em outra thread.</item>
    /// </list>
    /// </summary>
    internal sealed class SilenceWatch
    {
        private readonly Heartbeat heartbeat;
        private readonly PingLedger ledger = new PingLedger();
        private readonly ConnectionTiming timing;
        private readonly IMonotonicClock clock;
        private readonly MainThreadQueue queue;
        private MonotonicInstant lastTick;
        private int generation;
        private bool watching;
        private bool confirmationPending;

        internal SilenceWatch(ConnectionTiming timing, IMonotonicClock clock, MainThreadQueue queue)
        {
            this.timing = timing;
            this.clock = clock;
            this.queue = queue;
            heartbeat = new Heartbeat(timing.PingInterval.TotalSeconds, timing.SilenceLimit.TotalSeconds);
        }

        /// <summary>Hora de mandar um ping.</summary>
        internal event Action? PingDue;

        /// <summary>O silêncio passou do limite e nenhum frame na fila o desmentiu.</summary>
        internal event Action<TimeSpan>? SilenceConfirmed;

        /// <summary>Um pong casou com um ping pendente.</summary>
        internal event Action<TimeSpan>? LatencyMeasured;

        /// <summary>Socket novo aberto: tudo recomeça, e o timeout só arma no primeiro pong dele.</summary>
        internal void Start()
        {
            generation++;
            watching = true;
            confirmationPending = false;
            heartbeat.Reset();
            ledger.Clear();
            lastTick = clock.Now;
        }

        /// <summary>O socket acabou; uma confirmação que ainda esteja na fila é ignorada.</summary>
        internal void Stop()
        {
            generation++;
            watching = false;
            confirmationPending = false;
        }

        internal PingMessage NextPing()
        {
            heartbeat.NotePingSent();
            return new PingMessage(ledger.Next(clock.Now));
        }

        internal void NoteFrame(ServerFrame frame)
        {
            if (!(frame is PongFrame pong))
            {
                heartbeat.NoteInbound();
                return;
            }

            heartbeat.NotePong();
            TimeSpan? latency = ledger.Match(pong.Marker, clock.Now);
            if (latency.HasValue)
                LatencyMeasured?.Invoke(latency.Value);
        }

        /// <summary>Volta ao primeiro plano: o tempo fora não conta como silêncio.</summary>
        internal void Forgive()
        {
            heartbeat.ForgivePause();
            lastTick = clock.Now;
        }

        internal void OnTick()
        {
            if (!watching)
                return;

            MonotonicInstant now = clock.Now;
            TimeSpan delta = now - lastTick;
            lastTick = now;
            if (delta > timing.PauseThreshold)
            {
                // App parado (carregamento, debugger, segundo plano sem aviso): não é o servidor mudo.
                heartbeat.ForgivePause();
                PingDue?.Invoke();
                return;
            }

            Evaluate(heartbeat.Tick(delta.TotalSeconds));
        }

        private void Evaluate(HeartbeatAction action)
        {
            if (action == HeartbeatAction.SendPing)
                PingDue?.Invoke();
            else if (action == HeartbeatAction.DeclareDead && !confirmationPending)
                EnqueueConfirmation();
        }

        private void EnqueueConfirmation()
        {
            confirmationPending = true;
            int confirming = generation;
            queue.Enqueue(() => Confirm(confirming));
        }

        private void Confirm(int confirming)
        {
            if (confirming != generation || !watching)
                return;

            confirmationPending = false;
            if (heartbeat.SilenceSeconds >= timing.SilenceLimit.TotalSeconds)
                SilenceConfirmed?.Invoke(TimeSpan.FromSeconds(heartbeat.SilenceSeconds));
        }
    }
}
