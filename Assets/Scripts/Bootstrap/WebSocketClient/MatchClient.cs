using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchClient : BaseClient
{
    private readonly Dictionary<string, Action<string>> handlers;

    string MatchId;

    public MatchClient(string baseUrl)
        : base(baseUrl)
    {
        handlers = new Dictionary<string, Action<string>>
        {
            { "match_start", HandleStartMatch }
            // adicione outros tipos aqui
        };
    }

    public void SetMatchId(string matchId)
    {
        MatchId = matchId;
    }

    protected override string BuildUrl()
    {
        // Mantém o baseUrl e adiciona token + matchId na query string
        return $"{this.baseUrl}?token={this.token}&matchId={this.MatchId}";
    }

    protected override void Handle(string type, string payload)
    {


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
  
    }
}
