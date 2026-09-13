
using UnityEngine;


public class NetworkBootstrap : MonoBehaviour
{
    public static MatchmakingClient MatchmakingClient { get; private set; }
    public static MatchClient MatchClient { get; private set; }


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        var config = AppEnvManager.Settings;

        // A rota ws/connection/ saiu do backend; o ConnectionClient deixou de ser criado
        // (specs/001-server-connection/plan.md, "Codigo anterior tocado").
        MatchmakingClient = new MatchmakingClient(
            config.WsUrl(config.matchmakingConsumerUrl),
            QueuePolicy());

        MatchClient = new MatchClient(
            config.WsUrl(config.matchConsumerUrl),
            MatchPolicy());

        // Aqui, e nao num RuntimeInitializeOnLoadMethod: e o unico ponto em que
        // os clients ja existem e a ordem e garantida.
        ReconnectOverlay.Attach(MatchmakingClient, MatchClient);
    }

    /// <summary>
    /// Fila: desiste rapido. Fila velha nao vale nada, e insistir em silencio e
    /// pior que devolver o jogador para a Home e deixar ele clicar de novo.
    /// </summary>
    private static ReconnectPolicy QueuePolicy()
    {
        return new ReconnectPolicy(
            baseDelaySeconds: 0.5,
            maxDelaySeconds: 5.0,
            maxAttempts: 5);
    }

    /// <summary>
    /// Partida: o mais agressivo dos tres. Perder a partida por queda de rede e
    /// o pior resultado possivel, e o estado vive 6h no Redis esperando a volta.
    /// </summary>
    private static ReconnectPolicy MatchPolicy()
    {
        return new ReconnectPolicy(
            baseDelaySeconds: 0.5,
            maxDelaySeconds: 15.0);
    }
}
