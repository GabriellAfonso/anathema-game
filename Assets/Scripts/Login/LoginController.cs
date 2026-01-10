using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Multiplayer.Playmode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        return JsonUtility.ToJson(new LoginRequestDTO
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
        var response = JsonUtility.FromJson<LoginResponseDTO>(jsonResponse);

        Debug.Log($"Login bem-sucedido no ambiente: {AppEnvManager.Settings.name}");
        var connectionClient = NetworkBootstrap.ConnectionClient;

        connectionClient.OnConnected += HandleSocketConnected;
        connectionClient.OnConnectionError += HandleSocketError;

        PlayerSession.Instance.SetToken(response.token);
        SelfProfileService.Instance.LoadProfile(response.token);

        connectionClient.Connect();
    }

    private void HandleSocketConnected()
    {
        NetworkBootstrap.ConnectionClient.OnConnected -= HandleSocketConnected;
        NetworkBootstrap.ConnectionClient.OnConnectionError -= HandleSocketError;

      


        SceneManager.LoadScene("HomeScene");
    }

    private void HandleSocketError(string error)
    {
        NetworkBootstrap.ConnectionClient.OnConnected -= HandleSocketConnected;
        NetworkBootstrap.ConnectionClient.OnConnectionError -= HandleSocketError;

        Debug.LogError($"Erro ao conectar no WebSocket: {error}");
    }

    private void SetLoginInteractable(bool value)
    {
        loginButton.interactable = value;
    }

    #region Auto Login Gambiarra
    [System.Serializable]
    private class DevUser
    {
        public string username;
        public string password;
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Start()

    {
        if (CurrentPlayer.ReadOnlyTags().Contains("Player1"))
        {
            AutoLoginDev(0);
        }
        else if (CurrentPlayer.ReadOnlyTags().Contains("Player2"))
        {
            AutoLoginDev(1);
        }
            
    }
   

    private void AutoLoginDev(int player)
    {
        var devUsers = new DevUser[]
           {
            new() { username = "teste1", password = "123456" },
            new() { username = "teste7", password = "123456" }
           };


        var user = devUsers[player];
        StartCoroutine(SendLoginRequest(user.username, user.password));
    }
    #endif
    #endregion

}
