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
        //Debug.Log("Acordou o envmanger");
        Settings = isProd ? configProd : configDev;

        print($"AppEnvManager: Loaded {(isProd ? "Production" : "Development")} Config");

        DontDestroyOnLoad(gameObject);
    }
}
