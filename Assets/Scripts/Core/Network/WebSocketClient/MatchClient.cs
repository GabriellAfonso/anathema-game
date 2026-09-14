#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

/// <summary>
/// Socket da partida no codigo de cena. A conexao abre com matchId e token, desiste sozinha no
/// match_denied e reconecta recebendo match_start de novo. O que se faz com o payload de match_start
/// continua como antes; tipar esse frame e o espelho da partida sao da feature 4.
/// </summary>
/// <example><code>PlayerSession.Instance.Match.Connect(pairing.Match);</code></example>
public class MatchClient : BaseClient
{
    private const string MatchSceneName = "MatchScene";
    private const string MatchStartType = "match_start";

    private readonly Uri matchBase;
    private readonly IClientLog log;
    private bool nextTextIsMatchStart;

    /// <summary>Cliente sobre a conexao de partida.</summary>
    /// <example><code>MatchClient match = new MatchClient(connections.MatchConnection, connections.Routes.Match, log);</code></example>
    public MatchClient(AuthenticatedConnection connection, Uri matchBase, IClientLog log)
        : base(connection)
    {
        this.matchBase = matchBase ?? throw new ArgumentNullException(nameof(matchBase), "match base url is null: expected ConnectionRoutes.Match");
        this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        connection.FrameReceived += frame => nextTextIsMatchStart = frame.MessageType == MatchStartType;
        connection.RawTextReceived += OnRawText;
    }

    /// <summary>Conecta ao socket da partida pareada e passa a se manter nele.</summary>
    /// <example><code>match.Connect(pairing.Match);</code></example>
    public void Connect(MatchId match)
    {
        Connection.Connect(ConnectionTarget.Match(matchBase, match));
    }

    private void OnRawText(string text)
    {
        // Ponte ate a feature 4: o match_start ainda vira MatchStateDTO por JsonConvert, como antes
        // (specs/003-authenticated-socket-queue/research.md, R12).
        if (!nextTextIsMatchStart)
            return;

        nextTextIsMatchStart = false;
        HandleStartMatch(JsonConvert.DeserializeObject<MatchStartEnvelope>(text)?.Payload);
    }

    /// <summary>
    /// Chega no inicio da partida e de novo a cada reconexao, com o estado
    /// atual. Precisa ser idempotente: recarregar a cena numa reconexao
    /// apagaria a partida que o jogador estava vendo.
    /// </summary>
    private void HandleStartMatch(MatchStateDTO? state)
    {
        if (state == null)
        {
            log.Error("match_start_without_state");
            return;
        }

        // Antes de carregar: a cena le o estado da sessao quando sobe, entao
        // ele precisa ja estar la.
        MatchSession.Instance.ApplyState(state);

        if (SceneManager.GetActiveScene().name == MatchSceneName)
            return;

        SceneManager.LoadScene(MatchSceneName);
    }

    private sealed class MatchStartEnvelope
    {
        [JsonProperty("payload")]
        public MatchStateDTO? Payload { get; set; }
    }
}
