#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Unity;
using UnityEngine;

// Fica no namespace global: o codigo antigo (NetworkBootstrap, LoginController, AppEnvManager)
// usa AppConfig sem using, e esta feature evolui o arquivo no lugar sem tocar neles.
[CreateAssetMenu(
    fileName = "AppConfig",
    menuName = "Config/App Config"
)]
public class AppConfig : ScriptableObject
{
    [Header("API")]
    [Tooltip("Host e porta, SEM esquema. Ex: 127.0.0.1:8000 ou api.anathema.com")]
    public string apiBaseUrl = "";

    [Tooltip("Em producao deve ser sempre true: usa https:// e wss://.")]
    public bool useTls;

    [Header("Endpoints")]
    public string loginEndpoint = "";
    public string playerMe = "";
    public string tokenRefreshEndpoint = "";
    public string registerEndpoint = "";
    public string cardsEndpoint = "";
    public string decksEndpoint = "";
    public string matchesEndpoint = "";

    // connectionConsumerUrl saiu: a rota ws/connection/ nao existe mais no backend.
    [Header("WebSocket")]
    public string matchmakingConsumerUrl = "";
    public string matchConsumerUrl = "";

    // Estado so de execucao. [NonSerialized] porque, ao entrar em Play, o Unity serializa ate os
    // campos privados de objetos em memoria e nao representa string nula: overrideHost voltava ""
    // e toda URL saia "http:///..." (UriFormatException no login ao dar Play).
    [System.NonSerialized] private string? overrideHost;
    [System.NonSerialized] private bool launchHostApplied;

    /// <summary>
    /// Host usado nas URLs. Em build de desenvolvimento, o host passado na inicializacao
    /// (extra de intent <c>serverHost</c> no Android, <c>-serverHost</c> no Windows) vale no
    /// lugar de <see cref="apiBaseUrl"/>: no celular, <c>localhost</c> e o proprio aparelho.
    /// </summary>
    /// <example><code>string host = AppEnvManager.Settings.EffectiveHost; // 192.168.0.10:8000</code></example>
    public string EffectiveHost
    {
        get
        {
            ApplyLaunchHostOnce();
            return string.IsNullOrEmpty(overrideHost) ? apiBaseUrl : overrideHost!;
        }
    }

    /// <summary>
    /// Troca o host em tempo de execucao. So build de desenvolvimento aceita; devolve o motivo
    /// da recusa, ou nulo quando aceitou.
    /// </summary>
    /// <example><code>string? problem = config.OverrideHost("192.168.0.10:8000");</code></example>
    public string? OverrideHost(string raw) => OverrideHost(raw, Debug.isDebugBuild);

    internal string? OverrideHost(string raw, bool developmentBuild)
    {
        if (!developmentBuild)
            return $"server host override '{raw}' refused: only development builds accept a runtime host";

        if (!ServerHost.TryParse(raw, out ServerHost? host, out string problem))
            return problem;

        overrideHost = host.HostAndPort;
        return null;
    }

    /// <summary>URL HTTP de uma rota no host efetivo.</summary>
    /// <example><code>string login = config.HttpUrl(config.loginEndpoint);</code></example>
    public string HttpUrl(string path)
    {
        return $"{(useTls ? "https" : "http")}://{EffectiveHost}{path}";
    }

    /// <summary>URL WebSocket de uma rota no host efetivo.</summary>
    /// <example><code>string matchmaking = config.WsUrl(config.matchmakingConsumerUrl);</code></example>
    public string WsUrl(string path)
    {
        return $"{(useTls ? "wss" : "ws")}://{EffectiveHost}{path}";
    }

    /// <summary>
    /// Rotas HTTP de conta e dados do jogador no host efetivo. Rota vazia lança com o campo e o
    /// asset, para o erro de configuração aparecer na composição e não no primeiro pedido.
    /// </summary>
    /// <example><code>AccountRoutes routes = AppEnvManager.Settings.BuildAccountRoutes();</code></example>
    public AccountRoutes BuildAccountRoutes()
    {
        return new AccountRoutes(
            RouteUrl(registerEndpoint, nameof(registerEndpoint)),
            RouteUrl(loginEndpoint, nameof(loginEndpoint)),
            RouteUrl(tokenRefreshEndpoint, nameof(tokenRefreshEndpoint)),
            RouteUrl(playerMe, nameof(playerMe)),
            RouteUrl(cardsEndpoint, nameof(cardsEndpoint)),
            RouteUrl(decksEndpoint, nameof(decksEndpoint)),
            RouteUrl(matchesEndpoint, nameof(matchesEndpoint)));
    }

    private System.Uri RouteUrl(string path, string field)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new System.InvalidOperationException($"{field} is '{path}' in {name}: expected a route like /accounts/login/");

        return new System.Uri(HttpUrl(path));
    }

    private void ApplyLaunchHostOnce()
    {
        if (launchHostApplied)
            return;

        launchHostApplied = true;
        string? launchHost = LaunchServerHostReader.ReadLaunchValue(LaunchServerHostReader.ServerHostName);
        string? problem = launchHost == null ? null : OverrideHost(launchHost);
        if (problem != null)
            new UnityConsoleLog().Warning("server_host_rejected", new LogField("raw", launchHost!), new LogField("problem", problem));
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
            return;

        if (ServerHost.TryParse(apiBaseUrl, out ServerHost? host, out _))
            apiBaseUrl = host.HostAndPort;
    }
}
