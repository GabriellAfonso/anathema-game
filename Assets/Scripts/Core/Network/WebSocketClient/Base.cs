using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

/// <summary>Em que ponto do ciclo de conexao o client esta.</summary>
public enum ClientConnectionState
{
    /// <summary>Nunca conectou, ou saiu de proposito.</summary>
    Disconnected,

    /// <summary>Handshake em andamento.</summary>
    Connecting,

    /// <summary>Socket aberto.</summary>
    Connected,

    /// <summary>Caiu, esperando o delay do backoff.</summary>
    WaitingRetry,

    /// <summary>Renovando o access token antes da proxima tentativa.</summary>
    RefreshingToken,

    /// <summary>Desistiu. So um Connect() explicito recomeca.</summary>
    GaveUp,
}

public abstract class BaseClient
{
    protected WebSocket socket;

    protected readonly string baseUrl;
    protected string token;

    private readonly ReconnectPolicy policy;
    private readonly Heartbeat heartbeat;

    private double retryCountdown;
    private bool retryArmed;
    private bool refreshBeforeRetry;

    // O close chega por dois caminhos que podem correr juntos: o callback
    // OnClose e a excecao de socket.Connect(). Sem esta trava, uma queda
    // agendaria duas reconexoes.
    private bool closeHandled;

    private bool leftOnPurpose;

    // Preenchido pelos handlers de recusa do servidor (match_denied e afins).
    // Tem prioridade sobre o codigo de close, que a NativeWebSocket achata.
    private string rejectionReason;

    // Ligado por auth_denied: o servidor recusou este access token.
    private bool tokenRejected;

    // Ligado no primeiro evento que nao seja auth_denied: o servidor tratou
    // este socket como autenticado.
    private bool sessionProven;

    /// <summary>
    /// Evento que todo consumer manda quando o gate de autenticacao recusa,
    /// antes de fechar com 4001 (BaseConsumer.deny_unauthenticated).
    /// </summary>
    private const string AuthDeniedEvent = "auth_denied";

    /// <summary>Prova de vida que o cliente manda; o servidor devolve <c>pong</c>.</summary>
    private const string PingEvent = "ping";
    private const string PongEvent = "pong";

    public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;

    protected bool isConnected => State == ClientConnectionState.Connected;

    /// <summary>Socket aberto. Dispara tambem em cada reconexao.</summary>
    public event Action OnConnected;

    public event Action<string> OnConnectionError;

    /// <summary>Caiu e vai voltar: numero da tentativa e quantos segundos faltam.</summary>
    public event Action<int, double> OnReconnecting;

    /// <summary>Voltou depois de ter caido. Hora de pedir o estado ao servidor.</summary>
    public event Action OnReconnected;

    /// <summary>Nao vai mais tentar. O motivo serve de mensagem para o jogador.</summary>
    public event Action<string> OnGaveUp;

    protected BaseClient(
        string baseUrl,
        ReconnectPolicy policy = null,
        Heartbeat heartbeat = null)
    {
        this.baseUrl = baseUrl;
        this.policy = policy ?? new ReconnectPolicy();
        this.heartbeat = heartbeat ?? new Heartbeat();
    }

    // ===== Ciclo de vida =====

    /// <summary>
    /// Conecta, e passa a reconectar sozinho ate a politica mandar desistir.
    /// </summary>
    public void Connect()
    {
        if (State == ClientConnectionState.Connecting || State == ClientConnectionState.Connected)
            return;

        leftOnPurpose = false;
        rejectionReason = null;
        refreshBeforeRetry = false;
        policy.Reset();

        WebSocketDispatcher.Instance.Register(this);

        // A abertura acontece no proximo Tick, nunca no frame de quem chamou:
        // Connect() costuma vir de dentro de um handler de mensagem.
        State = ClientConnectionState.Connecting;
        retryCountdown = 0;
        retryArmed = true;
    }

    /// <summary>Sai de proposito. Nao reconecta.</summary>
    public void Disconnect()
    {
        leftOnPurpose = true;
        retryArmed = false;
        State = ClientConnectionState.Disconnected;

        WebSocketDispatcher.Instance.Unregister(this);
        CloseSocket();
    }

    /// <summary>
    /// Bombeia mensagens e conta o tempo do backoff. Chamado pelo
    /// <see cref="WebSocketDispatcher"/> a cada frame.
    /// </summary>
    internal void Pump(float deltaSeconds)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
#endif

        // Depois de bombear: uma mensagem entregue neste frame ja conta como
        // prova de vida e evita um DeclareDead na fronteira do timeout.
        if (State == ClientConnectionState.Connected)
            PulseHeartbeat(deltaSeconds);

        if (!retryArmed)
            return;

        retryCountdown -= deltaSeconds;

        if (retryCountdown > 0)
            return;

        retryArmed = false;

        if (refreshBeforeRetry)
        {
            refreshBeforeRetry = false;
            RefreshTokenThenOpen();
            return;
        }

        OpenSocket();
    }

    private void PulseHeartbeat(float deltaSeconds)
    {
        switch (heartbeat.Tick(deltaSeconds))
        {
            case HeartbeatAction.SendPing:
                Send(PingEvent);
                break;

            case HeartbeatAction.DeclareDead:
                DeclareDead();
                break;
        }
    }

    /// <summary>
    /// O socket parou de responder sem fechar. Derruba na mao para a reconexao
    /// ter o que reagir: TCP meio-aberto nunca gera OnClose sozinho.
    /// </summary>
    private void DeclareDead()
    {
        Debug.LogWarning(
            $"{GetType().Name}: {heartbeat.SilenceSeconds:0}s sem resposta, derrubando a conexao.");

        var dead = socket;

        // Fecha primeiro: o OnClose que o cancelamento provoca chega depois,
        // e a trava closeHandled o descarta em vez de agendar duas vezes.
        HandleClose(WebSocketCloseCode.Abnormal);

        dead?.CancelConnection();
    }

    private async void OpenSocket()
    {
        State = ClientConnectionState.Connecting;

        // Descrevem o socket da vez, nao a sessao: zera a cada tentativa.
        closeHandled = false;
        tokenRejected = false;
        sessionProven = false;
        rejectionReason = null;
        heartbeat.Reset();

        this.token = PlayerSession.Instance.Token;

        socket = new WebSocket(BuildUrl());

        socket.OnOpen += HandleOpen;
        socket.OnClose += HandleClose;
        socket.OnError += HandleError;
        socket.OnMessage += OnMessage;

        try
        {
            // Na implementacao nativa isto so retorna quando a conexao acaba.
            await socket.Connect();
        }
        catch (Exception error)
        {
            OnConnectionError?.Invoke(error.Message);
            HandleClose(WebSocketCloseCode.Abnormal);
        }
    }

    private async void CloseSocket()
    {
        if (socket == null)
            return;

        var closing = socket;
        socket = null;

        try
        {
            // Close() da NativeWebSocket so age em socket aberto. Sair no meio
            // do handshake exige cancelar, senao o loop de recepcao fica vivo.
            if (closing.State == WebSocketState.Open)
                await closing.Close();
            else
                closing.CancelConnection();
        }
        catch (Exception error)
        {
            Debug.LogWarning($"{GetType().Name}: erro ao fechar o socket: {error.Message}");
        }
    }

    private void RefreshTokenThenOpen()
    {
        State = ClientConnectionState.RefreshingToken;

        TokenRefreshService.Instance.Refresh(result =>
        {
            switch (result)
            {
                case TokenRefreshResult.Success:
                    OpenSocket();
                    break;

                case TokenRefreshResult.Expired:
                    GiveUp("sessao expirada: e preciso entrar de novo");
                    break;

                default:
                    // Falha de rede nao e token invalido: nao gasta tentativa de
                    // auth, so espera o proximo passo do backoff.
                    Schedule(policy.OnClosed(ReconnectPolicy.AbnormalClosure));
                    break;
            }
        });
    }

    // ===== Eventos do socket =====

    private void HandleOpen()
    {
        var voltou = policy.Attempt > 0;

        closeHandled = false;

        // So o backoff. O socket abrir nao prova que o token vale: o gate de
        // autenticacao aceita justamente para poder mandar o auth_denied.
        policy.ResetBackoff();

        State = ClientConnectionState.Connected;

        OnConnected?.Invoke();

        if (voltou)
            OnReconnected?.Invoke();

        OnOpen();
    }

    private void HandleClose(WebSocketCloseCode code)
    {
        if (closeHandled)
            return;

        closeHandled = true;

        OnClose(code);

        if (leftOnPurpose)
        {
            State = ClientConnectionState.Disconnected;
            return;
        }

        // O servidor recusou por regra e avisou por mensagem. Vale mais que o
        // codigo: a NativeWebSocket colapsa tudo fora de 1000-1015 em Undefined.
        if (rejectionReason != null)
        {
            GiveUp(rejectionReason);
            return;
        }

        // Mesmo motivo: o 4001 chegaria aqui como Undefined. Quem sabe que foi
        // o token e a mensagem auth_denied, recebida antes do close.
        var closeCode = tokenRejected ? ReconnectPolicy.AuthRejected : (int)code;

        Schedule(policy.OnClosed(closeCode));
    }

    private void HandleError(string error)
    {
        OnError(error);
        OnConnectionError?.Invoke(error);
    }

    // ===== Reconexao =====

    private void Schedule(ReconnectPlan plan)
    {
        if (plan.Action == ReconnectAction.GiveUp)
        {
            GiveUp(plan.Reason);
            return;
        }

        refreshBeforeRetry = plan.Action == ReconnectAction.RefreshTokenThenRetry;

        retryCountdown = plan.DelaySeconds;
        retryArmed = true;
        State = ClientConnectionState.WaitingRetry;

        Debug.Log($"{GetType().Name}: {plan}");

        OnReconnecting?.Invoke(policy.Attempt, plan.DelaySeconds);
    }

    private void GiveUp(string reason)
    {
        retryArmed = false;
        refreshBeforeRetry = false;
        State = ClientConnectionState.GaveUp;

        WebSocketDispatcher.Instance.Unregister(this);

        Debug.LogWarning($"{GetType().Name}: parei de reconectar: {reason}");

        OnGaveUp?.Invoke(reason);
    }

    /// <summary>
    /// Marca que o servidor recusou por regra, e nao por falha de rede. O
    /// proximo close desiste em vez de tentar de novo.
    ///
    /// Chamado pelos handlers de recusa: o consumer manda a mensagem de erro e
    /// so depois fecha, justamente para o motivo chegar.
    /// </summary>
    protected void RejectReconnect(string reason)
    {
        rejectionReason = reason;
    }

    protected virtual string BuildUrl()
    {
        return $"{this.baseUrl}?token={this.token}";
    }

    // ===== Hooks das subclasses =====

    protected virtual void OnOpen()
    {
    }

    protected virtual void OnClose(WebSocketCloseCode code)
    {
    }

    protected virtual void OnError(string error)
    {
    }

    protected virtual void OnMessage(byte[] bytes)
    {
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        Dispatch(json);
    }

    // ===== Protocolo =====

    /// <summary>
    /// O servidor envia {"type": "...", "payload": {...}}, com payload sendo
    /// objeto aninhado e nao string (BaseConsumer.send_event). O payload volta
    /// para os handlers como JSON cru para cada um desserializar no seu DTO.
    /// </summary>
    protected virtual void Dispatch(string json)
    {
        JObject envelope;

        try
        {
            envelope = JObject.Parse(json);
        }
        catch (JsonException e)
        {
            Debug.LogError($"{GetType().Name}: frame invalido descartado: {e.Message}");
            return;
        }

        var type = envelope.Value<string>("type");

        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning($"{GetType().Name}: frame sem 'type' descartado.");
            return;
        }

        var payload = envelope["payload"];
        var body = payload?.ToString(Formatting.None) ?? "{}";

        // Chegou frame: o cano esta vivo, seja qual for o conteudo.
        heartbeat.NoteInbound();

        // Os eventos abaixo valem para qualquer consumer, entao nao descem para
        // as subclasses: elas so registrariam o mesmo handler tres vezes.

        if (type == PongEvent)
        {
            heartbeat.NotePong();
            return;
        }

        if (type == AuthDeniedEvent)
        {
            HandleAuthDenied(body);
            return;
        }

        // Qualquer outro evento so chega em socket autenticado: e a prova de
        // que o token vale, e o unico ponto onde limpar o historico de auth.
        if (!sessionProven)
        {
            sessionProven = true;
            policy.Reset();
        }

        Handle(type, body);
    }

    /// <summary>
    /// O gate de autenticacao recusou este access token. Marca a proxima
    /// reconexao para renovar o token antes de tentar.
    ///
    /// Casa pelo tipo do evento, nunca pelo texto: a mensagem e diagnostico e
    /// muda conforme o usuario do scope.
    /// </summary>
    private void HandleAuthDenied(string payload)
    {
        var dto = JsonUtility.FromJson<ErrorPayloadDTO>(payload);
        var reason = string.IsNullOrEmpty(dto?.error) ? "motivo nao informado" : dto.error;

        Debug.LogWarning($"{GetType().Name}: token recusado pelo servidor: {reason}");

        tokenRejected = true;
    }

    protected virtual void Handle(string type, string payload)
    {
        // sobrescrito nos clients concretos
    }

    // ===== Envio =====

    protected void Send(string type, object payload = null)
    {
        if (!isConnected)
        {
            // Descarte silencioso e como jogada perdida vira bug sem rastro.
            Debug.LogWarning($"{GetType().Name}: '{type}' descartado, socket em {State}.");
            return;
        }

        var message = new JObject
        {
            ["type"] = type,
            ["payload"] = payload == null ? new JObject() : JToken.FromObject(payload),
        };

        socket.SendText(message.ToString(Formatting.None));
    }
}
