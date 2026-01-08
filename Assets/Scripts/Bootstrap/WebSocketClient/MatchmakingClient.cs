using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;


public class MatchmakingClient : BaseClient
{
    private readonly Dictionary<string, Action<string>> handlers;

    

    public MatchmakingClient(string baseUrl)
        : base(baseUrl)
    {
        handlers = new Dictionary<string, Action<string>>
        {
            { "match_found", HandleStartMatch }
            // adicione outros tipos aqui
        };
    }

    protected override void Handle(string type, string payload)
    {
        
        Debug.Log("entrou no handler "+type);

        if (handlers.TryGetValue(type, out var handler))
        {
            handler(payload);
        }
        else
        {
            Console.WriteLine($"No handler for type: {type}");
        }
    }

    private void HandleStartMatch(string payload)
    {
        Debug.Log(payload);
        // criar dados versusContext
        VersusContext.Instance.SetContext(payload);

        // starta a cena de versus
        SceneManager.LoadScene("VersusScene");
        // manda conectar o matchClient ao matchcConsumer
    }
}
