using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class LoginController : MonoBehaviour
{
    // Removido: [SerializeField] private AppConfig appConfig; 
    // Agora não precisa mais arrastar nada no Inspector!

    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;

    public void OnLoginClicked()
    {
        // Verifica se a variável global foi inicializada pelo Bootstrap
        if (AppEnvManager.Settings == null)
        {
            Debug.LogError("Configuração Global não encontrada! O Bootstrap rodou primeiro?");
            return;
        }

        string username = usernameInput.text;
        string password = passwordInput.text;

        StartCoroutine(LoginRequest(username, password));
    }

    private IEnumerator LoginRequest(string username, string password)
    {
        // ACESSO GLOBAL: Pega os dados direto do Manager estático
        string urlBase = AppEnvManager.Settings.apiBaseUrl;
        string endpoint = AppEnvManager.Settings.loginEndpoint;
        string loginUrl = urlBase + endpoint;

        LoginPayload payload = new LoginPayload
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(loginUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        loginButton.interactable = false;

        yield return request.SendWebRequest();

        loginButton.interactable = true;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Erro no login ({AppEnvManager.Settings.name}): {request.error}");
            yield break;
        }

        Debug.Log($"Login bem-sucedido no ambiente: {AppEnvManager.Settings.name}");
        var connectionClient = WebSocketClient.connectionClient;
        Debug.Log("vai conectar");
        //connectionClient.Connect();
    }

    [System.Serializable]
    private class LoginPayload
    {
        public string username;
        public string password;
    }
}