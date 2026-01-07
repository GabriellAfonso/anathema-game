using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        loginButton.onClick.AddListener(HandleLogin);

        usernameInput.onSubmit.AddListener(_ => HandleLogin());
        passwordInput.onSubmit.AddListener(_ => HandleLogin());
    }

    private void HandleLogin()
    {
        if (!IsEnvironmentReady())
            return;

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Usuário ou senha vazios.");
            return;
        }

        StartCoroutine(SendLoginRequest(username, password));
    }

    private bool IsEnvironmentReady()
    {
        if (AppEnvManager.Settings == null)
        {
            Debug.LogError("Configuração global não encontrada. O Bootstrap foi executado?");
            return false;
        }

        return true;
    }

    private IEnumerator SendLoginRequest(string username, string password)
    {
        string loginUrl = BuildLoginUrl();
        string jsonBody = BuildLoginRequestDto(username, password);

        using UnityWebRequest request = CreatePostRequest(loginUrl, jsonBody);

        SetLoginInteractable(false);

        yield return request.SendWebRequest();

        SetLoginInteractable(true);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Erro no login ({AppEnvManager.Settings.name}): {request.error}");
            yield break;
        }

        HandleLoginSuccess(request.downloadHandler.text);
    }

    private string BuildLoginUrl()
    {
        return $"http://{AppEnvManager.Settings.apiBaseUrl}{AppEnvManager.Settings.loginEndpoint}";
    }

    private string BuildLoginRequestDto(string username, string password)
    {
        return JsonUtility.ToJson(new LoginRequestDto
        {
            username = username,
            password = password
        });
    }

    private UnityWebRequest CreatePostRequest(string url, string jsonBody)
    {
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private void HandleLoginSuccess(string jsonResponse)
    {
        var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

        Debug.Log($"Login bem-sucedido no ambiente: {AppEnvManager.Settings.name}");
        var connectionClient = WebSocketClient.connectionClient;

        connectionClient.OnConnected += HandleSocketConnected;
        connectionClient.OnConnectionError += HandleSocketError;


        SelfProfileService.Instance.LoadProfile(response.token);
        connectionClient.Connect(response.token);
    }

    private void HandleSocketConnected()
    {
        WebSocketClient.connectionClient.OnConnected -= HandleSocketConnected;
        WebSocketClient.connectionClient.OnConnectionError -= HandleSocketError;

      


        SceneManager.LoadScene("Home");
    }

    private void HandleSocketError(string error)
    {
        WebSocketClient.connectionClient.OnConnected -= HandleSocketConnected;
        WebSocketClient.connectionClient.OnConnectionError -= HandleSocketError;

        Debug.LogError($"Erro ao conectar no WebSocket: {error}");
    }

    private void SetLoginInteractable(bool value)
    {
        loginButton.interactable = value;
    }

    [System.Serializable]
    private class LoginRequestDto
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
        public string refresh;
    }
}
