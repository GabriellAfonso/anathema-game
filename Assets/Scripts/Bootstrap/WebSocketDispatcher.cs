using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketDispatcher : MonoBehaviour
{
    public static WebSocketDispatcher Instance { get; private set; }
    private readonly List<WebSocket> sockets = new();
    private readonly List<WebSocket> toAdd = new();
    private readonly List<WebSocket> toRemove = new();


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
        if (!toAdd.Contains(socket))
            toAdd.Add(socket);
    }

    public void Unregister(WebSocket socket)
    {
        if (!toRemove.Contains(socket))
            toRemove.Add(socket);
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        foreach (var socket in sockets)
        {
            socket?.DispatchMessageQueue();
        }

        if (toAdd.Count > 0)
        {
            sockets.AddRange(toAdd);
            toAdd.Clear();
        }

        if (toRemove.Count > 0)
        {
            foreach (var s in toRemove)
                sockets.Remove(s);
            toRemove.Clear();
        }
#endif
    }

}
