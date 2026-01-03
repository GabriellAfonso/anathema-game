using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneBootstrapper
{
    private const string BOOTSTRAP_SCENE_KEY = "BootstrapScenePath";
    private const string LAST_SCENE_KEY = "LastOpenedScenePath";

    static SceneBootstrapper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Salva o caminho da cena que você está tentando testar
            string currentScene = EditorSceneManager.GetActiveScene().path;
            EditorPrefs.SetString(LAST_SCENE_KEY, currentScene);
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            string bootstrapPath = GetBootstrapScenePath();
            string currentScene = SceneManager.GetActiveScene().path;

            // Se não estivermos no bootstrap, forçamos a ida para lá
            if (currentScene != bootstrapPath)
            {
                EditorSceneManager.LoadSceneInPlayMode(bootstrapPath, new LoadSceneParameters(LoadSceneMode.Single));
            }
        }
    }

    private static string GetBootstrapScenePath()
    {
        // Pega a primeira cena do Build Settings (que deve ser o Bootstrap)
        if (EditorBuildSettings.scenes.Length > 0)
            return EditorBuildSettings.scenes[0].path;

        return string.Empty;
    }
}