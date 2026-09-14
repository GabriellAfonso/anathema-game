#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Unity;
using TMPro;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela de login. Ao abrir, tenta retomar a sessão guardada sem pedir senha; se não der, espera
/// usuário e senha e entra pela sessão de conta (specs/002-player-account). Nos dois caminhos lê o
/// perfil e vai para a Home.
/// </summary>
public class LoginController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput = null!;
    [SerializeField] private TMP_InputField passwordInput = null!;
    [SerializeField] private Button loginButton = null!;

    private static LiveAccountServices Account => PlayerSession.Instance.Account;

    private static IClientLog Log => PlayerSession.Instance.Log;

    private void Awake()
    {
        loginButton.onClick.AddListener(HandleLogin);

        usernameInput.onSubmit.AddListener(_ => HandleLogin());
        passwordInput.onSubmit.AddListener(_ => HandleLogin());
    }

    private async void Start()
    {
        if (!IsEnvironmentReady())
            return;

        try
        {
            await ResumeOrWaitForLoginAsync();
        }
        catch (Exception unexpected)
        {
            ReportUnexpected("login_resume_failed", unexpected);
        }
    }

    private async Task ResumeOrWaitForLoginAsync()
    {
        SetLoginInteractable(false);
        ResumeOutcome resumed = await Account.Session.ResumeAsync();
        if (resumed.Kind == ResumeOutcomeKind.Resumed)
        {
            await EnterHomeAsync();
            return;
        }

        SetLoginInteractable(true);
        StartDevAutoLogin();
    }

    private async void HandleLogin()
    {
        if (!IsEnvironmentReady())
            return;

        try
        {
            await SignInWithInputsAsync();
        }
        catch (Exception unexpected)
        {
            ReportUnexpected("login_failed_unexpectedly", unexpected);
        }
    }

    private Task SignInWithInputsAsync()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            return SignInAsync(username, password);

        Log.Warning("login_input_empty");
        return Task.CompletedTask;
    }

    private async Task SignInAsync(string username, string password)
    {
        SetLoginInteractable(false);
        SignInOutcome outcome = await Account.Session.SignInAsync(username, new Password(password));
        if (outcome.Kind == SignInOutcomeKind.SignedIn)
        {
            await EnterHomeAsync();
            return;
        }

        // Um segundo toque durante o login em curso não pode religar o botão antes de ele terminar.
        if (outcome.Kind != SignInOutcomeKind.AlreadyInProgress)
            SetLoginInteractable(true);
    }

    private static async Task EnterHomeAsync()
    {
        // O MiniPlayerProfile lê a sessão no Awake da Home: sem esperar o perfil, ele aparecia vazio.
        // Falha ao ler o perfil fica registrada pelo SelfProfileService e não impede a Home.
        await SelfProfileService.Instance.LoadProfileAsync();

        // O socket de presenca (ws/connection/) saiu do backend; a Home nao espera mais por ele.
        SceneManager.LoadScene("HomeScene");
    }

    private static bool IsEnvironmentReady()
    {
        if (AppEnvManager.Settings != null && PlayerSession.Instance != null)
            return true;

        new UnityConsoleLog().Error("login_environment_missing", new LogField("expected", "BootstrapScene ran AppEnvManager and PlayerSession"));
        return false;
    }

    private void ReportUnexpected(string eventName, Exception unexpected)
    {
        Log.Error(eventName, new LogField("error", unexpected.GetType().Name));
        SetLoginInteractable(true);
    }

    private void SetLoginInteractable(bool value)
    {
        loginButton.interactable = value;
    }

    #region Auto Login Gambiarra
    [System.Serializable]
    private class DevUser
    {
        public string username = "";
        public string password = "";
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void StartDevAutoLogin()
    {
        if (CurrentPlayer.ReadOnlyTags().Contains("Player1"))
            AutoLoginDev(0);
        else if (CurrentPlayer.ReadOnlyTags().Contains("Player2"))
            AutoLoginDev(1);
    }

    private async void AutoLoginDev(int player)
    {
        DevUser[] devUsers =
        {
            new DevUser { username = "teste1", password = "123456" },
            new DevUser { username = "teste7", password = "123456" },
        };

        DevUser user = devUsers[player];
        try
        {
            await SignInAsync(user.username, user.password);
        }
        catch (Exception unexpected)
        {
            ReportUnexpected("dev_auto_login_failed", unexpected);
        }
    }
#else
    private void StartDevAutoLogin()
    {
    }
#endif
    #endregion
}
