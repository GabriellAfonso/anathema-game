using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Da tempo de execucao aos clients: bombeia as mensagens recebidas e conta o
/// backoff da reconexao, um passo por frame.
///
/// Registra o client, e nao o socket: a cada reconexao nasce um WebSocket novo,
/// e o client e quem sobrevive a isso.
/// </summary>
public class WebSocketDispatcher : MonoBehaviour
{
    private static WebSocketDispatcher instance;

    /// <summary>
    /// Cria o dispatcher se ele nao estiver em cena. Sem ele nenhum client
    /// recebe mensagem, e faltar o objeto na cena daria NullReferenceException
    /// no primeiro Connect().
    /// </summary>
    public static WebSocketDispatcher Instance
    {
        get
        {
            if (instance != null)
                return instance;

            var host = new GameObject(nameof(WebSocketDispatcher));
            DontDestroyOnLoad(host);

            // AddComponent roda o Awake na hora, que ja preenche `instance`.
            host.AddComponent<WebSocketDispatcher>();

            return instance;
        }
    }

    private readonly List<BaseClient> clients = new();
    private readonly List<BaseClient> toAdd = new();
    private readonly List<BaseClient> toRemove = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(BaseClient client)
    {
        if (client == null)
            return;

        toRemove.Remove(client);

        if (!clients.Contains(client) && !toAdd.Contains(client))
            toAdd.Add(client);
    }

    public void Unregister(BaseClient client)
    {
        if (client == null)
            return;

        toAdd.Remove(client);

        if (clients.Contains(client) && !toRemove.Contains(client))
            toRemove.Add(client);
    }

    private void Update()
    {
        // Tempo nao escalado: com timeScale em 0 numa pausa ou num overlay de
        // reconexao, a contagem do backoff precisa continuar andando.
        var delta = Time.unscaledDeltaTime;

        // Itera antes de aplicar as listas: um Pump pode registrar ou remover
        // client, e mutar a colecao no meio do foreach quebraria.
        foreach (var client in clients)
            client?.Pump(delta);

        if (toAdd.Count > 0)
        {
            clients.AddRange(toAdd);
            toAdd.Clear();
        }

        if (toRemove.Count > 0)
        {
            foreach (var client in toRemove)
                clients.Remove(client);

            toRemove.Clear();
        }
    }
}
