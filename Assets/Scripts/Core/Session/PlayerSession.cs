#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Unity;
using UnityEngine;

/// <summary>
/// Raiz de composição da conta no jogo e ponte com o código anterior. No Awake monta a camada de
/// rede (fila da thread principal, adaptadores, NetworkLayerHost) e a sessão de conta. O
/// BaseClient ainda lê <see cref="Token"/> e o MiniPlayerProfile ainda lê o perfil daqui; o
/// singleton sai quando a feature 3 religar os dois nas portas novas
/// (specs/002-player-account/plan.md, Complexity Tracking).
/// </summary>
/// <example><code>LiveAccountServices account = PlayerSession.Instance.Account;</code></example>
public class PlayerSession : MonoBehaviour
{
    /// <summary>A sessão da BootstrapScene (dívida registrada até a feature 3).</summary>
    /// <example><code>string? token = PlayerSession.Instance.Token;</code></example>
    public static PlayerSession Instance { get; private set; } = null!;

    /// <summary>Conta do jogador, composta no Awake.</summary>
    /// <example><code>SignInOutcome outcome = await PlayerSession.Instance.Account.Session.SignInAsync(username, password);</code></example>
    public LiveAccountServices Account { get; private set; } = null!;

    /// <summary>Log da camada, para o código antigo registrar sem Debug.Log.</summary>
    /// <example><code>PlayerSession.Instance.Log.Warning("login_input_empty");</code></example>
    public IClientLog Log { get; private set; } = null!;

    /// <summary>
    /// Texto do access token atual, sem renovar; nulo sem sessão. O BaseClient lê ao abrir o socket e
    /// pede renovação ao TokenRefreshService depois de um 4001. O refresh token não sai mais daqui:
    /// ele é o único jeito de sobreviver aos 5 minutos do access token, e fica na guarda segura.
    /// </summary>
    /// <example><code>string? token = PlayerSession.Instance.Token;</code></example>
    public string? Token => Account?.Session.CurrentAccessTokenText;

    /// <summary>Apelido do perfil lido.</summary>
    /// <example><code>nickname.text = session.Nickname;</code></example>
    public string Nickname { get; private set; } = "";

    /// <summary>Chave do ícone do perfil lido.</summary>
    /// <example><code>SetIcon(session.Icon);</code></example>
    public string Icon { get; private set; } = "";

    /// <summary>Nível do perfil lido.</summary>
    /// <example><code>levelText.text = $"Lv: {session.Level}";</code></example>
    public int Level { get; private set; }

    /// <summary>Experiência do perfil lido.</summary>
    /// <example><code>int experience = session.Experience_points;</code></example>
    public int Experience_points { get; private set; }

    /// <summary>Moedas do perfil lido.</summary>
    /// <example><code>coinsText.text = session.Coins.ToString();</code></example>
    public int Coins { get; private set; }

    /// <summary>Créditos do perfil lido.</summary>
    /// <example><code>int credits = session.Credits;</code></example>
    public int Credits { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ComposeAccount();
    }

    /// <summary>Copia o perfil lido para os campos que a Home mostra.</summary>
    /// <example><code>PlayerSession.Instance.SetProfile(profile.Value);</code></example>
    public void SetProfile(OwnProfile profile)
    {
        Nickname = profile.Nickname;
        Icon = profile.Icon;
        Level = (int)profile.Level;
        Experience_points = (int)profile.ExperiencePoints;
        Coins = (int)profile.Coins;
        Credits = (int)profile.Credits;
    }

    private void ComposeAccount()
    {
        UnityConsoleLog log = new UnityConsoleLog();
        MainThreadQueue queue = new MainThreadQueue(log);
        LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(Debug.isDebugBuild));
        UnityAppLifecycle lifecycle = UnityAppLifecycle.Create(adapters.Clock, queue);
        // O UnityHttpTransport completa as tarefas pela fila; sem o hospedeiro drenando, nenhum login termina.
        gameObject.AddComponent<NetworkLayerHost>().Attach(queue, lifecycle, UnityNetworkReachability.Create(adapters.Clock, queue));
        Log = log;
        Account = LiveAccountServices.FromAdapters(adapters, lifecycle, AppEnvManager.Settings.BuildAccountRoutes(), PlatformRefreshTokenVault.Create(log));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Account?.Dispose();
    }
}
