using UnityEngine;
using UnityEngine.SceneManagement;

public class AppEnvManager : MonoBehaviour
{
    public static AppConfig Settings { get; private set; }

    [SerializeField] private AppConfig configDev;
    [SerializeField] private AppConfig configProd;
    [SerializeField] private bool isProd;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Settings = isProd ? configProd : configDev;

        if (Settings == null)
        {
            Debug.LogError($"AppEnvManager: config {(isProd ? "configProd" : "configDev")} nao foi atribuido no Inspector.");
            return;
        }

        if (string.IsNullOrEmpty(Settings.apiBaseUrl))
            Debug.LogError($"AppEnvManager: apiBaseUrl vazio em {Settings.name}.");

        if (isProd && !Settings.useTls)
            Debug.LogError($"AppEnvManager: config de producao ({Settings.name}) esta com useTls desligado. Token JWT vai trafegar em texto puro.");

        print($"AppEnvManager: Loaded {(isProd ? "Production" : "Development")} Config");
    }
}
