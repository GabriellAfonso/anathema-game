using UnityEngine;

[CreateAssetMenu(
    fileName = "AppConfig",
    menuName = "Config/App Config"
)]
public class AppConfig : ScriptableObject
{
    [Header("API")]
    public string apiBaseUrl;

    [Header("Endpoints")]
    public string loginEndpoint;
}
