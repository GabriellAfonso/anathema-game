#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using UnityEngine.SceneManagement;

/// <summary>
/// Fila no codigo de cena: entra com um deck, e no pareamento leva a VersusScene e conecta o socket de
/// partida. Reentrada automatica, recusas e desistencia moram na <see cref="MatchQueue"/>.
/// </summary>
/// <example><code>PlayerSession.Instance.Matchmaking.Join(deck);</code></example>
public class MatchmakingClient : BaseClient
{
    private const string VersusSceneName = "VersusScene";

    private readonly MatchQueue queue;
    private readonly MatchClient match;
    private readonly IClientLog log;

    /// <summary>Cliente sobre a conexao e a fila de fila, e o cliente de partida que abre no pareamento.</summary>
    /// <example><code>MatchmakingClient matchmaking = new MatchmakingClient(connections.MatchmakingConnection, connections.Queue, match, log);</code></example>
    public MatchmakingClient(AuthenticatedConnection connection, MatchQueue queue, MatchClient match, IClientLog log)
        : base(connection)
    {
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the MatchQueue over the matchmaking connection");
        this.match = match ?? throw new ArgumentNullException(nameof(match), "match client is null: expected the MatchClient that opens after match_found");
        this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        queue.Paired += HandleMatchFound;
        queue.Refused += HandleRefused;
        queue.MatchmakingFailed += HandleMatchmakingFailed;
        queue.LeftQueue += HandleLeftQueue;
    }

    /// <summary>Fase da fila.</summary>
    /// <example><code>bool searching = matchmaking.Phase == QueuePhase.Searching;</code></example>
    public QueuePhase Phase => queue.Phase;

    /// <summary>Entra na fila com o deck.</summary>
    /// <example><code>JoinOutcome outcome = matchmaking.Join(deck);</code></example>
    public JoinOutcome Join(DeckId deck) => queue.Join(deck);

    /// <summary>Sai da fila.</summary>
    /// <example><code>matchmaking.Leave();</code></example>
    public void Leave() => queue.Leave();

    private void HandleMatchFound(MatchPairing pairing)
    {
        // criar dados versusContext
        VersusContext.Instance.SetContext(pairing);

        // starta a cena de versus
        SceneManager.LoadScene(VersusSceneName);

        // manda conectar o matchClient ao matchConsumer
        match.Connect(pairing.Match);
    }

    private void HandleRefused(QueueRefusal refusal)
    {
        log.Warning("matchmaking_join_refused", new LogField("code", refusal.Code), new LogField("problems", refusal.Problems.Count));
    }

    /// <summary>
    /// O pareamento aconteceu mas o perfil de um dos jogadores nao existe. O
    /// socket segue aberto: o servidor nao fecha, so avisa.
    /// </summary>
    private void HandleMatchmakingFailed(string error)
    {
        log.Error("matchmaking_failed", new LogField("error", error));
    }

    private void HandleLeftQueue(GiveUpReason reason)
    {
        log.Warning("matchmaking_left_queue", new LogField("kind", reason.Kind.ToString()));
    }
}
