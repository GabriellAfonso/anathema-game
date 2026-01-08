
using UnityEngine;


public class WebSocketClient : MonoBehaviour
{
    public static ConnectionClient ConnectionClient { get; private set; }
    public static MatchmakingClient MatchmakingClient { get; private set; }
    public static MatchClient MatchClient { get; private set; }


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        string apiBaseUrl = AppEnvManager.Settings.apiBaseUrl;
        string connectionUrl = AppEnvManager.Settings.connectionConsumerUrl;
        string matchmakingUrl = AppEnvManager.Settings.matchmakingConsumerUrl;
        string matchUrl = AppEnvManager.Settings.matchConsumerUrl;

        ConnectionClient = new ConnectionClient("ws://" + apiBaseUrl + connectionUrl);
        MatchmakingClient = new MatchmakingClient("ws://" + apiBaseUrl + matchmakingUrl);
        MatchClient = new MatchClient("ws://" + apiBaseUrl + matchUrl);
    }
  
}
