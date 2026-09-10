
using UnityEngine;


public class NetworkBootstrap : MonoBehaviour
{
    public static ConnectionClient ConnectionClient { get; private set; }
    public static MatchmakingClient MatchmakingClient { get; private set; }
    public static MatchClient MatchClient { get; private set; }


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        var config = AppEnvManager.Settings;

        ConnectionClient = new ConnectionClient(
            config.WsUrl(config.connectionConsumerUrl),
            PresencePolicy());

        MatchmakingClient = new MatchmakingClient(
            config.WsUrl(config.matchmakingConsumerUrl),
            QueuePolicy());

        MatchClient = new MatchClient(
            config.WsUrl(config.matchConsumerUrl),
            MatchPolicy());
    }

    /// <summary>
    /// Presenca: tenta para sempre, sem pressa. Cair daqui nao atrapalha
    /// nenhuma acao do jogador.
    /// </summary>
    private static ReconnectPolicy PresencePolicy()
    {
        return new ReconnectPolicy(
            baseDelaySeconds: 1.0,
            maxDelaySeconds: 60.0);
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
