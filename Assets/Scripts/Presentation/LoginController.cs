#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Client.Scenes;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using TMPro;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de login. Ao receber a fachada, tenta retomar a sessão guardada sem pedir senha; se não der, espera
/// usuário e senha e entra pela conta da fachada (specs/002-player-account). Nos dois caminhos quem leva à Home é o
/// roteador de cena, quando o estágio muda (specs/005-presentation-facade/contracts/presentation-surface.md).
/// </summary>
/// <example><code>// Componente da LoginScene; o hospedeiro chama BindClient.</code></example>
public class LoginController : MonoBehaviour, ISceneClientConsumer
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput = null!;
    [SerializeField] private TMP_InputField passwordInput = null!;
    [SerializeField] private Button loginButton = null!;

    private AnathemaClient? client;

    private void Awake()
    {
        loginButton.onClick.AddListener(HandleLogin);

        usernameInput.onSubmit.AddListener(_ => HandleLogin());
        passwordInput.onSubmit.AddListener(_ => HandleLogin());
        SetLoginInteractable(false);
    }

    /// <summary>Recebe a fachada e tenta retomar a sessão guardada.</summary>
    /// <example><code>loginController.BindClient(client);</code></example>
    public void BindClient(AnathemaClient bound)
    {
        client = bound;
        if (bound.State.SignedOutReason == SignedOutReason.SessionExpired)
            bound.Log.Warning("login_after_session_expired");

        _ = ResumeOrWaitForLoginAsync(bound);
    }

    private async Task ResumeOrWaitForLoginAsync(AnathemaClient bound)
    {
        try
        {
            ResumeOutcome resumed = await bound.Account.ResumeAsync();
            // Retomou: o roteador leva à Home quando o estágio muda; o socket de presença (ws/connection/) saiu do
            // backend e a Home não espera por ele.
            if (resumed.Kind == ResumeOutcomeKind.Resumed)
                return;

            SetLoginInteractable(true);
            StartDevAutoLogin();
        }
        catch (Exception unexpected)
        {
            ReportUnexpected("login_resume_failed", unexpected);
        }
    }

    private void HandleLogin()
    {
        if (client == null)
            return;

        _ = SignInWithInputsAsync(client);
    }

    private Task SignInWithInputsAsync(AnathemaClient bound)
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            return SignInAsync(bound, username, password);

        bound.Log.Warning("login_input_empty");
        return Task.CompletedTask;
    }

    private async Task SignInAsync(AnathemaClient bound, string username, string password)
    {
        try
        {
            SetLoginInteractable(false);
            SignInOutcome outcome = await bound.Account.SignInAsync(username, new Password(password));
            // Um segundo toque durante o login em curso não pode religar o botão antes de ele terminar.
            if (outcome.Kind != SignInOutcomeKind.SignedIn && outcome.Kind != SignInOutcomeKind.AlreadyInProgress && outcome.Kind != SignInOutcomeKind.AlreadySignedIn)
                SetLoginInteractable(true);
        }
        catch (Exception unexpected)
        {
            ReportUnexpected("login_failed_unexpectedly", unexpected);
        }
    }

    private void ReportUnexpected(string eventName, Exception unexpected)
    {
        client?.Log.Error(eventName, new LogField("error", unexpected.GetType().Name));
        SetLoginInteractable(true);
    }

    private void SetLoginInteractable(bool value)
    {
        if (loginButton != null)
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

    private void AutoLoginDev(int player)
    {
        DevUser[] devUsers =
        {
            new DevUser { username = "teste1", password = "123456" },
            new DevUser { username = "teste7", password = "123456" },
        };

        DevUser user = devUsers[player];
        if (client != null)
            _ = SignInAsync(client, user.username, user.password);
    }
#else
    private void StartDevAutoLogin()
    {
    }
#endif
    #endregion
}
