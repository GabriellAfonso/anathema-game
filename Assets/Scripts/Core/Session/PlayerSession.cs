#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Unity;
using UnityEngine;

/// <summary>
/// Raiz de composição da camada de rede nas cenas. No Awake monta a fila da thread principal, os
/// adaptadores, o NetworkLayerHost, a conta, as conexões de fila e de partida e os clientes de socket de
/// cena, que recebem tudo pelo construtor. Só o código de cena lê este singleton; ele sai quando a
/// feature 5 der a fachada à apresentação (specs/003-authenticated-socket-queue/plan.md, Complexity Tracking).
/// </summary>
/// <example><code>LiveAccountServices account = PlayerSession.Instance.Account;</code></example>
public class PlayerSession : MonoBehaviour
{
    private LiveConnectionServices? connections;

    /// <summary>A sessão da BootstrapScene (dívida registrada até a feature 5).</summary>
    /// <example><code>MatchmakingClient matchmaking = PlayerSession.Instance.Matchmaking;</code></example>
    public static PlayerSession Instance { get; private set; } = null!;

    /// <summary>Conta do jogador, composta no Awake.</summary>
    /// <example><code>SignInOutcome outcome = await PlayerSession.Instance.Account.Session.SignInAsync(username, password);</code></example>
    public LiveAccountServices Account { get; private set; } = null!;

    /// <summary>Cliente de fila das cenas.</summary>
    /// <example><code>PlayerSession.Instance.Matchmaking.Join(deck);</code></example>
    public MatchmakingClient Matchmaking { get; private set; } = null!;

    /// <summary>Cliente de partida das cenas.</summary>
    /// <example><code>PlayerSession.Instance.Match.Connect(pairing.Match);</code></example>
    public MatchClient Match { get; private set; } = null!;

    /// <summary>Log da camada, para o código antigo registrar sem Debug.Log.</summary>
    /// <example><code>PlayerSession.Instance.Log.Warning("login_input_empty");</code></example>
    public IClientLog Log { get; private set; } = null!;

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
        ComposeNetwork();
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

    private void ComposeNetwork()
    {
        UnityConsoleLog log = new UnityConsoleLog();
        MainThreadQueue queue = new MainThreadQueue(log);
        LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(Debug.isDebugBuild));
        UnityAppLifecycle lifecycle = UnityAppLifecycle.Create(adapters.Clock, queue);
        UnityNetworkReachability reachability = UnityNetworkReachability.Create(adapters.Clock, queue);
        UnityFrameTicker ticker = new UnityFrameTicker();
        // O UnityHttpTransport completa as tarefas pela fila; sem o hospedeiro drenando, nenhum login termina.
        gameObject.AddComponent<NetworkLayerHost>().Attach(queue, lifecycle, reachability, ticker);
        Log = log;
        Account = LiveAccountServices.FromAdapters(adapters, lifecycle, AppEnvManager.Settings.BuildAccountRoutes(), PlatformRefreshTokenVault.Create(log));
        ComposeSocketClients(adapters, lifecycle, reachability, ticker);
    }

    private void ComposeSocketClients(LiveNetworkAdapters adapters, UnityAppLifecycle lifecycle, UnityNetworkReachability reachability, UnityFrameTicker ticker)
    {
        LiveConnectionServices composed = LiveConnectionServices.FromAdapters(adapters, lifecycle, reachability, ticker, Account.Tokens, AppEnvManager.Settings.BuildConnectionRoutes());
        connections = composed;
        Match = new MatchClient(composed.MatchConnection, composed.Routes.Match, Account.Catalog, adapters.Clock, Log);
        Matchmaking = new MatchmakingClient(composed.MatchmakingConnection, composed.Queue, Match, Log);

        // Aqui, e nao num RuntimeInitializeOnLoadMethod: e o unico ponto em que
        // os clients ja existem e a ordem e garantida.
        ReconnectOverlay.Attach(Matchmaking, Match);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        connections?.Dispose();
        Account?.Dispose();
    }
}
