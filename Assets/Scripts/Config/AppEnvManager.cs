using UnityEngine;
using UnityEngine.SceneManagement;

public class AppEnvManager : MonoBehaviour
{
    public static AppConfig Settings { get; private set; }

    [Header("Config Assets")]
    [SerializeField] private AppConfig configDev;
    [SerializeField] private AppConfig configProd;
    public string ambienteDesejado = "Dev";

    private void Awake()
    {
        // 1. Configuração Global
        Settings = (ambienteDesejado.ToLower() == "prod") ? configProd : configDev;
        DontDestroyOnLoad(gameObject);

        // 2. Lógica de Redirecionamento Inteligente
        string sceneToLoad = "";

#if UNITY_EDITOR
        // No Editor, volta para a cena onde você deu Play
        sceneToLoad = UnityEditor.EditorPrefs.GetString("LastOpenedScenePath", "");
#endif

        // Se não houver cena salva (ou for o build final), vai para a cena de índice 1
        if (string.IsNullOrEmpty(sceneToLoad) || sceneToLoad.Contains("Bootstrap"))
        {
            // Carrega a segunda cena da lista do Build Settings (geralmente o Login/Menu)
            SceneManager.LoadScene(1);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}