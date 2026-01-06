using System;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public abstract class BaseClient
{
    protected WebSocket socket;

    protected readonly string baseUrl;
    protected string token;

    protected bool isConnected => socket != null && socket.State == WebSocketState.Open;

    protected BaseClient(string baseUrl)
    {
        this.baseUrl = baseUrl;
    }

    public event Action OnConnected;
    public event Action<string> OnConnectionError;
    // ===== Conexão =====

    public async void Connect(string token)
    {

        if (isConnected)
            return;

        this.token = token;

        var url = BuildUrl();
        socket = new WebSocket(url);

        socket.OnOpen += () =>
        {
            OnConnected?.Invoke();
        };

        socket.OnClose += OnClose;
        socket.OnError += (e) =>
        {
            OnConnectionError?.Invoke(e);
        };
        socket.OnMessage += OnMessage;

        await socket.Connect();
    }

    public async void Disconnect()
    {
        if (socket == null)
            return;

        await socket.Close();
        socket = null;
    }

   protected string BuildUrl()
    {
        return $"{this.baseUrl}?token={this.token}";
    }

    // ===== Eventos base =====

    protected virtual void OnOpen()
    {
        Debug.Log($"{GetType().Name} connected");
    }

    protected virtual void OnClose(WebSocketCloseCode code)
    {
        Debug.Log($"{GetType().Name} disconnected: {code}");
    }

    protected virtual void OnError(string error)
    {
        Debug.LogError($"{GetType().Name} error: {error}");
    }

    protected virtual void OnMessage(byte[] bytes)
    {
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        Dispatch(json);
    }

    // ===== Protocolo =====

    protected virtual void Dispatch(string json)
    {
        var message = JsonUtility.FromJson<BaseMessage>(json);

        if (string.IsNullOrEmpty(message.type))
            return;

        Handle(message.type, message.payload);
    }

    protected virtual void Handle(string type, string payload)
    {
        // sobrescrito nos clients concretos
    }

    // ===== Envio =====

    protected void Send(string type, object payload = null)
    {
        if (!isConnected)
            return;

        var message = new OutgoingMessage
        {
            type = type,
            payload = payload
        };

        socket.SendText(JsonUtility.ToJson(message));
    }

    // ===== DTOs =====

    [Serializable]
    protected class BaseMessage
    {
        public string type;
        public string payload;
    }

    [Serializable]
    protected class OutgoingMessage
    {
        public string type;
        public object payload;
    }
}


