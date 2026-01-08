using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketDispatcher : MonoBehaviour
{
    public static WebSocketDispatcher Instance { get; private set; }

    private readonly List<WebSocket> sockets = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(WebSocket socket)
    {
        if (!sockets.Contains(socket))
            sockets.Add(socket);
    }

    public void Unregister(WebSocket socket)
    {
        sockets.Remove(socket);
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        foreach (var socket in sockets)
        {
            socket?.DispatchMessageQueue();
        }
#endif
    }
}
