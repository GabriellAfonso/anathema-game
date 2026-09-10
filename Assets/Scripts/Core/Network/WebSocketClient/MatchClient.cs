using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchClient : BaseClient
{
    private readonly Dictionary<string, Action<string>> handlers;

    string MatchId;

    public MatchClient(
        string baseUrl,
        ReconnectPolicy policy = null,
        Heartbeat heartbeat = null)
        : base(baseUrl, policy, heartbeat)
    {
        handlers = new Dictionary<string, Action<string>>
        {
            { "match_start", HandleStartMatch },
            { "match_denied", HandleMatchDenied },
            // adicione outros tipos aqui
        };
    }

    public void SetMatchId(string matchId)
    {
        MatchId = matchId;
    }

    protected override string BuildUrl()
    {
        // Mantem o baseUrl e adiciona token + matchId na query string
        return $"{this.baseUrl}?token={this.token}&matchId={this.MatchId}";
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

    private void HandleStartMatch(string payload)
    {
        SceneManager.LoadScene("MatchScene");
    }

    /// <summary>
    /// O MatchConsumer recusou a entrada: sem matchId, jogador nao participa,
    /// ou a partida nao existe mais. Reconectar nunca vai passar desse gate.
    ///
    /// A mensagem chega antes do close de proposito (MatchConsumer.reject), e e
    /// a unica forma confiavel de saber o motivo: a NativeWebSocket achata todo
    /// codigo fora de 1000-1015 em Undefined.
    /// </summary>
    private void HandleMatchDenied(string payload)
    {
        var reason = ReadError(payload);

        Debug.LogWarning($"{GetType().Name}: entrada na partida recusada: {reason}");

        RejectReconnect(reason);
    }

    private static string ReadError(string payload)
    {
        var dto = JsonUtility.FromJson<ErrorPayloadDTO>(payload);

        return string.IsNullOrEmpty(dto?.error) ? "motivo nao informado" : dto.error;
    }
}
