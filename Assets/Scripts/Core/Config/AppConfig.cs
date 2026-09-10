using UnityEngine;

[CreateAssetMenu(
    fileName = "AppConfig",
    menuName = "Config/App Config"
)]
public class AppConfig : ScriptableObject
{
    [Header("API")]
    [Tooltip("Host e porta, SEM esquema. Ex: 127.0.0.1:8000 ou api.anathema.com")]
    public string apiBaseUrl;

    [Tooltip("Em producao deve ser sempre true: usa https:// e wss://.")]
    public bool useTls;

    [Header("Endpoints")]
    public string loginEndpoint;
    public string playerMe;
    public string tokenRefreshEndpoint;

    [Header("WebSocket")]
    public string connectionConsumerUrl;
    public string matchmakingConsumerUrl;
    public string matchConsumerUrl;

    public string HttpUrl(string path)
    {
        return $"{(useTls ? "https" : "http")}://{apiBaseUrl}{path}";
    }

    public string WsUrl(string path)
    {
        return $"{(useTls ? "wss" : "ws")}://{apiBaseUrl}{path}";
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
            return;

        apiBaseUrl = apiBaseUrl
            .Replace("https://", string.Empty)
            .Replace("http://", string.Empty)
            .Replace("wss://", string.Empty)
            .Replace("ws://", string.Empty)
            .TrimEnd('/');
    }
}
