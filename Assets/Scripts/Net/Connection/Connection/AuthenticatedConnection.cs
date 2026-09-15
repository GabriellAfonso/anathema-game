#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Um socket lógico que se mantém sozinho: pede um token válido antes de cada abertura, renova
    /// quando o servidor recusa o token, desiste quando o gate da partida recusa ou a sessão expirou, e
    /// reconecta com backoff em qualquer outra queda. Frames chegam em ordem, na thread principal.
    /// Contrato: specs/003-authenticated-socket-queue/contracts/authenticated-connection.md.
    /// </summary>
    /// <example>
    /// <code>
    /// AuthenticatedConnection connection = new AuthenticatedConnection(ports, account.Tokens, codec, ConnectionSettings.ForMatch());
    /// connection.FrameReceived += OnFrame;
    /// connection.Connect(ConnectionTarget.Match(routes.Match, pairing.Match));
    /// </code>
    /// </example>
    internal sealed class AuthenticatedConnection : IDisposable
    {
        private readonly ConnectionPorts ports;
        private readonly IAccessTokenSource tokens;
        private readonly IProtocolCodec codec;
        private readonly ReconnectPolicy policy;
        private readonly SilenceWatch silence;
        private readonly ConnectionSuspension suspension;
        private ConnectionTarget? target;
        private SocketAttempt? socket;
        private MonotonicInstant? retryAt;
        private int generation;
        private int tokenRefusals;
        private bool recovering;
        private bool recyclePending;

        /// <summary>Conexão parada; nada acontece até <see cref="Connect"/>.</summary>
        /// <example><code>AuthenticatedConnection connection = new AuthenticatedConnection(ports, tokens, codec, ConnectionSettings.ForMatchmaking());</code></example>
        public AuthenticatedConnection(ConnectionPorts ports, IAccessTokenSource tokens, IProtocolCodec codec, ConnectionSettings settings)
        {
            this.ports = ports ?? throw new ArgumentNullException(nameof(ports), "ports are null: expected sockets, clock, ticker, lifecycle, reachability, queue and log");
            this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens), "token source is null: expected the account session's IAccessTokenSource");
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec), "codec is null: expected the protocol codec with ConnectionFrames.CreateUnion()");
            ConnectionSettings required = settings ?? throw new ArgumentNullException(nameof(settings), "settings are null: expected ConnectionSettings.ForMatchmaking() or ForMatch()");
            policy = required.Policy;
            silence = new SilenceWatch(required.Timing, ports.Clock, ports.Queue);
            silence.PingDue += SendPing;
            silence.SilenceConfirmed += OnSilenceConfirmed;
            silence.LatencyMeasured += OnLatencyMeasured;
            suspension = new ConnectionSuspension(ports.Lifecycle, ports.Reachability);
            suspension.Suspended += OnSuspended;
            suspension.Resumed += OnResumed;
            ports.Ticker.Ticked += OnTick;
        }

        /// <summary>Estado atual.</summary>
        /// <example><code>ConnectionPhase phase = connection.Status.Phase;</code></example>
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Of(ConnectionPhase.Disconnected);

        /// <summary>O estado mudou (FR-011).</summary>
        public event Action<ConnectionStatus>? StatusChanged;

        /// <summary>Frame decodificado do socket atual, em ordem (FR-014).</summary>
        public event Action<ServerFrame>? FrameReceived;

        /// <summary>A conexão voltou depois de cair: o socket reaberto foi provado (FR-012).</summary>
        public event Action? Recovered;

        /// <summary>Latência medida por um pong que casou com o ping (FR-018).</summary>
        public event Action<TimeSpan>? LatencyMeasured;

        /// <summary>A última latência medida; nula antes do primeiro pong casado.</summary>
        /// <example><code>TimeSpan? latency = connection.LastLatency;</code></example>
        public TimeSpan? LastLatency { get; private set; }

        /// <summary>Abre e passa a se manter aberta no alvo dado. Com a conexão já ativa no mesmo alvo, não faz nada.</summary>
        /// <example><code>connection.Connect(ConnectionTarget.Matchmaking(routes.Matchmaking));</code></example>
        public void Connect(ConnectionTarget next)
        {
            ConnectionTarget required = next ?? throw new ArgumentNullException(nameof(next), "target is null: expected ConnectionTarget.Matchmaking or ConnectionTarget.Match");
            if (IsActive)
            {
                RequireSameTarget(required);
                return;
            }

            target = required;
            policy.Reset();
            tokenRefusals = 0;
            recovering = false;
            BeginAttempt();
        }

        /// <summary>Sai de propósito: fecha e nunca reabre sozinha (FR-010).</summary>
        /// <example><code>connection.Leave();</code></example>
        public void Leave()
        {
            StopTrying();
            target = null;
            SetStatus(ConnectionStatus.Of(ConnectionPhase.Disconnected));
        }

        /// <summary>Manda uma mensagem pelo socket atual; sem socket aberto devolve <see cref="SocketSendOutcome.NotOpen"/> (FR-015).</summary>
        /// <example><code>SocketSendOutcome sent = await connection.SendAsync(new JoinQueueMessage(deck));</code></example>
        public Task<SocketSendOutcome> SendAsync(IOutgoingMessage message)
        {
            IOutgoingMessage required = message ?? throw new ArgumentNullException(nameof(message), "message is null: expected an IOutgoingMessage such as PingMessage or JoinQueueMessage");
            if (socket != null && socket.IsOpen)
                return socket.SendTextAsync(codec.Encode(required));

            ports.Log.Warning("connection_send_not_open", new LogField("message_type", required.MessageType), new LogField("phase", Status.Phase.ToString()));
            return Task.FromResult(SocketSendOutcome.NotOpen);
        }

        /// <summary>Para de ouvir quadros e fecha o socket, sem avisar mudança de estado.</summary>
        /// <example><code>connection.Dispose();</code></example>
        public void Dispose()
        {
            ports.Ticker.Ticked -= OnTick;
            suspension.Dispose();
            StopTrying();
        }

        private bool IsActive => Status.Phase != ConnectionPhase.Disconnected && Status.Phase != ConnectionPhase.GaveUp;

        private void RequireSameTarget(ConnectionTarget next)
        {
            if (!next.Equals(target))
                throw new InvalidOperationException($"connection is {Status.Phase} to {target}: expected Leave() before connecting to {next}");
        }

        private void BeginAttempt()
        {
            generation++;
            retryAt = null;
            if (suspension.IsSuspended)
            {
                Suspend();
                return;
            }

            SetStatus(ConnectionStatus.Of(ConnectionPhase.Connecting));
            _ = OpenWithFreshTokenAsync(generation);
        }

        private async Task OpenWithFreshTokenAsync(int attempt)
        {
            try
            {
                // Pedido imediatamente antes de abrir: um token lido antes de uma espera pode ter vencido nela.
                AccessTokenOutcome outcome = await tokens.GetValidAsync();
                if (!IsStale(attempt))
                    OnTokenOutcome(outcome);
            }
            catch (Exception unexpected)
            {
                FailAttempt(attempt, unexpected);
            }
        }

        private void OnTokenOutcome(AccessTokenOutcome outcome)
        {
            if (outcome.Kind == AccessTokenOutcomeKind.Valid)
            {
                OpenSocket(outcome.Token!);
                return;
            }

            if (outcome.Kind == AccessTokenOutcomeKind.SessionUnavailable)
            {
                GiveUp(outcome.Session == SessionUnavailableKind.Expired ? GiveUpReason.SessionExpired() : GiveUpReason.NoSession());
                return;
            }

            // Sem rede para renovar não é token recusado: espera o backoff sem contar recusa.
            recovering = true;
            ScheduleRetry(policy.OnClosed(ReconnectPolicy.AbnormalClosure), null);
        }

        private void OpenSocket(AccessToken token)
        {
            ConnectionTarget current = target ?? throw new InvalidOperationException($"opening a socket with no target while {Status.Phase}: expected Connect(target) first");
            DiscardSocket();
            SocketAttempt attempt = new SocketAttempt(ports.Sockets.Create(), codec, ports.Log);
            socket = attempt;
            attempt.Opened += () => OnOpened(attempt);
            attempt.Proven += () => OnProven(attempt);
            attempt.FrameArrived += frame => OnFrame(attempt, frame);
            attempt.Ended += end => OnEnded(attempt, end);
            ports.Log.Info("connection_opening", new LogField("target", current.ToString()), new LogField("attempt", policy.Attempt));
            attempt.Open(current.WithToken(token));
        }

        private void OnOpened(SocketAttempt attempt)
        {
            if (attempt != socket)
                return;

            silence.Start();
            SendPing();
            SetStatus(ConnectionStatus.Of(ConnectionPhase.Connected));
        }

        private void OnProven(SocketAttempt attempt)
        {
            if (attempt != socket)
                return;

            // Só a prova zera: o gate aceita o socket antes de recusar o token (FR-009).
            policy.Reset();
            tokenRefusals = 0;
            bool recovered = recovering;
            recovering = false;
            ports.Log.Info("connection_proven", new LogField("target", Describe()), new LogField("recovered", recovered));
            if (recovered)
                Recovered?.Invoke();
        }

        private void OnFrame(SocketAttempt attempt, ServerFrame frame)
        {
            if (attempt != socket)
                return;

            silence.NoteFrame(frame);
            FrameReceived?.Invoke(frame);
        }

        private void OnEnded(SocketAttempt attempt, SocketEnd end)
        {
            if (attempt != socket)
                return;

            socket = null;
            silence.Stop();
            recovering = true;
            if (end.Kind == SocketEndKind.MatchRefused)
                GiveUp(GiveUpReason.MatchRefused(end.Match ?? MatchRefusalDetail.Unspecified));
            else if (end.Kind == SocketEndKind.TokenRefused)
                OnTokenRefused();
            else
                ScheduleRetry(policy.OnClosed(end.CloseCode ?? ReconnectPolicy.AbnormalClosure), end.CloseCode);
        }

        private void OnTokenRefused()
        {
            tokenRefusals++;
            ReconnectPlan plan = policy.OnClosed(ReconnectPolicy.AuthRejected);
            if (plan.Action != ReconnectAction.GiveUp)
            {
                _ = RenewThenReopenAsync(generation);
                return;
            }

            GiveUp(policy.Attempt > policy.MaxAttempts ? GiveUpReason.AttemptsExhausted(policy.MaxAttempts) : GiveUpReason.TokenRefusedRepeatedly(tokenRefusals));
        }

        private async Task RenewThenReopenAsync(int attempt)
        {
            SetStatus(ConnectionStatus.Of(ConnectionPhase.RenewingToken));
            ports.Log.Info("connection_renewing_token", new LogField("refusals", tokenRefusals));
            try
            {
                RenewalOutcome renewal = await tokens.RenewNowAsync();
                if (!IsStale(attempt))
                    OnRenewal(renewal);
            }
            catch (Exception unexpected)
            {
                FailAttempt(attempt, unexpected);
            }
        }

        private void OnRenewal(RenewalOutcome renewal)
        {
            if (renewal.Kind == RenewalOutcomeKind.Renewed)
                BeginAttempt();
            else if (renewal.Kind == RenewalOutcomeKind.Unavailable)
                ScheduleRetry(policy.OnClosed(ReconnectPolicy.AbnormalClosure), null);
            else
                GiveUp(renewal.Kind == RenewalOutcomeKind.SessionExpired ? GiveUpReason.SessionExpired() : GiveUpReason.NoSession());
        }

        private void ScheduleRetry(ReconnectPlan plan, int? closeCode)
        {
            if (plan.Action == ReconnectAction.GiveUp)
            {
                GiveUp(GiveUpReason.AttemptsExhausted(policy.MaxAttempts));
                return;
            }

            TimeSpan wait = TimeSpan.FromSeconds(plan.DelaySeconds);
            retryAt = ports.Clock.Now.Add(wait);
            if (suspension.IsSuspended)
            {
                Suspend();
                return;
            }

            SetStatus(ConnectionStatus.Waiting(policy.Attempt, wait));
            ports.Log.Info("connection_waiting_retry", new LogField("attempt", policy.Attempt), new LogField("wait_ms", (long)wait.TotalMilliseconds),
                new LogField("close_code", closeCode.HasValue ? closeCode.Value.ToString() : "none"));
        }

        private void OnTick()
        {
            silence.OnTick();
            if (retryAt.HasValue && !suspension.IsSuspended && ports.Clock.Now >= retryAt.Value)
                BeginAttempt();
        }

        private void GiveUp(GiveUpReason reason)
        {
            StopTrying();
            SetStatus(ConnectionStatus.GivenUp(reason));
            ports.Log.Warning("connection_gave_up", new LogField("kind", reason.Kind.ToString()),
                new LogField("match_detail", reason.Match.HasValue ? reason.Match.Value.ToString() : "none"));
        }

        private void StopTrying()
        {
            generation++;
            retryAt = null;
            DiscardSocket();
        }

        private void DiscardSocket()
        {
            SocketAttempt? current = socket;
            socket = null;
            silence.Stop();
            current?.Discard();
        }

        private bool IsStale(int attempt)
        {
            if (attempt == generation)
                return false;

            ports.Log.Debug("connection_stale_token_result", new LogField("attempt_generation", attempt));
            return true;
        }

        private void FailAttempt(int attempt, Exception unexpected)
        {
            ports.Log.Error("connection_attempt_failed", new LogField("exception", unexpected.GetType().Name));
            if (attempt != generation)
                return;

            recovering = true;
            ScheduleRetry(policy.OnClosed(ReconnectPolicy.AbnormalClosure), null);
        }

        private void SetStatus(ConnectionStatus next)
        {
            if (next.Equals(Status))
                return;

            Status = next;
            StatusChanged?.Invoke(next);
        }

        private void SendPing()
        {
            if (socket != null && socket.IsOpen)
                _ = socket.SendTextAsync(codec.Encode(silence.NextPing()));
        }

        private void OnSilenceConfirmed(TimeSpan silent)
        {
            // TCP meio-aberto não fecha sozinho: derruba e segue o caminho de qualquer queda (FR-019).
            ports.Log.Warning("connection_silence_confirmed", new LogField("silence_ms", (long)silent.TotalMilliseconds));
            DiscardSocket();
            recovering = true;
            ScheduleRetry(policy.OnClosed(ReconnectPolicy.AbnormalClosure), null);
        }

        private void OnLatencyMeasured(TimeSpan latency)
        {
            LastLatency = latency;
            ports.Log.Debug("connection_latency", new LogField("latency_ms", (long)latency.TotalMilliseconds));
            LatencyMeasured?.Invoke(latency);
        }

        private void OnSuspended()
        {
            // Socket aberto não é fechado por ir para segundo plano; renovação ou abertura em curso terminam (FR-022).
            if (Status.Phase == ConnectionPhase.WaitingRetry)
                Suspend();
        }

        private void Suspend()
        {
            SuspensionReason reason = suspension.Reason ?? SuspensionReason.Background;
            if (Status.Phase == ConnectionPhase.Suspended && Status.Suspension == reason)
                return;

            SetStatus(ConnectionStatus.SuspendedBy(reason));
            ports.Log.Info("connection_suspended", new LogField("reason", reason.ToString()));
        }

        private void OnResumed(ResumeCause cause)
        {
            if (!IsActive)
                return;

            if (suspension.IsSuspended)
                recyclePending |= cause != ResumeCause.Foreground;
            else if (cause != ResumeCause.Foreground || recyclePending)
                ReopenNow(cause);
            else if (socket != null && socket.IsOpen)
                ConfirmOpenSocket();
            else if (Status.Phase == ConnectionPhase.WaitingRetry || Status.Phase == ConnectionPhase.Suspended)
                ReopenNow(cause);
        }

        private void ConfirmOpenSocket()
        {
            // Tempo pausado não é silêncio: zera antes de avaliar, e confirma a conexão com um ping (FR-023).
            silence.Forgive();
            ports.Log.Debug("connection_ping_on_foreground");
            SendPing();
        }

        private void ReopenNow(ResumeCause cause)
        {
            // O socket antigo não sobrevive à troca de interface, e a volta não espera o backoff:
            // nada disto consome tentativa (FR-023 a FR-025).
            bool hadSocket = socket != null;
            recyclePending = false;
            recovering = true;
            DiscardSocket();
            policy.ResetBackoff();
            LogReopening(hadSocket, cause);
            BeginAttempt();
        }

        private void LogReopening(bool hadSocket, ResumeCause cause)
        {
            NetworkKindChanged? change = suspension.LastChange;
            if (!hadSocket)
                ports.Log.Info("connection_reopening", new LogField("cause", cause.ToString()));
            else
                ports.Log.Info("connection_socket_recycled", new LogField("previous", change == null ? "none" : change.Previous.ToString()),
                    new LogField("current", change == null ? "none" : change.Current.ToString()));
        }

        private string Describe() => target?.ToString() ?? "none";
    }
}
