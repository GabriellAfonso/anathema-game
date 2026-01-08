using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ForceBootstrapOnPlay
{

    static ForceBootstrapOnPlay()
    {
        //Debug.Log("ForceBootstrapOnPlay carregado");
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Abre o bootstrap
                EditorSceneManager.OpenScene("Assets/Scenes/BootstrapScene.unity");

                // Depois abre a cena de login
                EditorSceneManager.OpenScene("Assets/Scenes/LoginScene.unity", OpenSceneMode.Additive);
            }
        };
    }
}
