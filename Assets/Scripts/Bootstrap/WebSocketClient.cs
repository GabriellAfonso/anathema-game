
using UnityEngine;


public class WebSocketClient : MonoBehaviour
{
    public static ConnectionClient connectionClient { get; private set; }


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        string apiBaseUrl = AppEnvManager.Settings.apiBaseUrl;
        string connectionUrl = AppEnvManager.Settings.connectionConsumerUrl;
        Debug.Log("ws://" + apiBaseUrl + connectionUrl);

        connectionClient = new ConnectionClient("ws://" + apiBaseUrl + connectionUrl);
    }
  
}
