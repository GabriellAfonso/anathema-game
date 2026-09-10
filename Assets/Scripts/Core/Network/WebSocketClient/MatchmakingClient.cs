using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchmakingClient : BaseClient
{
    private readonly Dictionary<string, Action<string>> handlers;

    public MatchmakingClient(
        string baseUrl,
        ReconnectPolicy policy = null,
        Heartbeat heartbeat = null)
        : base(baseUrl, policy, heartbeat)
    {
        handlers = new Dictionary<string, Action<string>>
        {
            { "match_found", HandleMatchFound },
            { "matchmaking_failed", HandleMatchmakingFailed },
            // adicione outros tipos aqui
        };
    }

    protected override void Handle(string type, string payload)
    {
        if (handlers.TryGetValue(type, out var handler))
        {
            handler(payload);
            return;
        }

        Debug.LogWarning($"{GetType().Name}: sem handler para o tipo '{type}'.");
    }

    private void HandleMatchFound(string payload)
    {
        // criar dados versusContext
        VersusContext.Instance.SetContext(payload);

        // starta a cena de versus
        SceneManager.LoadScene("VersusScene");

        // manda conectar o matchClient ao matchcConsumer
        var matchId = VersusContext.Instance.MatchId;
        var matchClient = NetworkBootstrap.MatchClient;

        matchClient.SetMatchId(matchId);
        matchClient.Connect();
    }

    /// <summary>
    /// O pareamento aconteceu mas o perfil de um dos jogadores nao existe. O
    /// socket segue aberto: o servidor nao fecha, so avisa.
    /// </summary>
    private void HandleMatchmakingFailed(string payload)
    {
        var dto = JsonUtility.FromJson<ErrorPayloadDTO>(payload);
        var reason = string.IsNullOrEmpty(dto?.error) ? "motivo nao informado" : dto.error;

        Debug.LogError($"{GetType().Name}: matchmaking falhou: {reason}");
    }
}
