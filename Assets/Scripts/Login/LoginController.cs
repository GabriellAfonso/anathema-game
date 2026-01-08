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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        DevPlayerIndex = pid % 2; // alterna entre 0 e 1
#endif
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
        var connectionClient = WebSocketClient.ConnectionClient;

        connectionClient.OnConnected += HandleSocketConnected;
        connectionClient.OnConnectionError += HandleSocketError;

        PlayerSession.Instance.SetToken(response.token);
        SelfProfileService.Instance.LoadProfile(response.token);

        connectionClient.Connect();
    }

    private void HandleSocketConnected()
    {
        WebSocketClient.ConnectionClient.OnConnected -= HandleSocketConnected;
        WebSocketClient.ConnectionClient.OnConnectionError -= HandleSocketError;

      


        SceneManager.LoadScene("HomeScene");
    }

    private void HandleSocketError(string error)
    {
        WebSocketClient.ConnectionClient.OnConnected -= HandleSocketConnected;
        WebSocketClient.ConnectionClient.OnConnectionError -= HandleSocketError;

        Debug.LogError($"Erro ao conectar no WebSocket: {error}");
    }

    private void SetLoginInteractable(bool value)
    {
        loginButton.interactable = value;
    }

    [Header("Dev Login (Editor Only)")]
    [SerializeField] private int DevPlayerIndex = 0; // 0 = player1, 1 = player2

    [System.Serializable]
    private class DevUser
    {
        public string username;
        public string password;
    }

    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AutoLoginDev();
#endif
    }

    private void AutoLoginDev()
    {
        var devUsers = new DevUser[]
        {
        new DevUser { username = "teste1", password = "123456" },
        new DevUser { username = "teste7", password = "123456" }
        };

        //if (DevPlayerIndex < 0 || DevPlayerIndex >= devUsers.Length)
        //    DevPlayerIndex = 0;

        var user = devUsers[DevPlayerIndex];
        Debug.Log($"Dev login automático: {user.username}");
        StartCoroutine(SendLoginRequest(user.username, user.password));
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
