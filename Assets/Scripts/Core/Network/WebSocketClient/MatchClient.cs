#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;
using UnityEngine.SceneManagement;

/// <summary>
/// Socket da partida no codigo de cena. Cria e hospeda a <see cref="LiveMatch"/> de cada partida pareada:
/// a sessao abre com matchId e token, desiste sozinha no match_denied, reconecta recebendo match_start de
/// novo e espelha o estado tipado (specs/004-match-session/contracts/legacy-bridge.md).
/// </summary>
/// <example><code>PlayerSession.Instance.Match.Connect(pairing.Match);</code></example>
public class MatchClient : BaseClient
{
    private const string MatchSceneName = "MatchScene";

    private readonly Uri matchBase;
    private readonly CardCatalog catalog;
    private readonly IMonotonicClock clock;
    private readonly IClientLog log;

    /// <summary>Cliente sobre a conexao de partida, com o catalogo e o relogio que a sessao precisa.</summary>
    /// <example><code>MatchClient match = new MatchClient(connections.MatchConnection, connections.Routes.Match, account.Catalog, adapters.Clock, log);</code></example>
    public MatchClient(AuthenticatedConnection connection, Uri matchBase, CardCatalog catalog, IMonotonicClock clock, IClientLog log)
        : base(connection)
    {
        this.matchBase = matchBase ?? throw new ArgumentNullException(nameof(matchBase), "match base url is null: expected ConnectionRoutes.Match");
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog), "catalog is null: expected the account CardCatalog");
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "clock is null: expected the monotonic clock of the adapters");
        this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
    }

    /// <summary>A sessao da partida atual, ou null antes do primeiro Connect.</summary>
    /// <example><code>LiveMatch? live = PlayerSession.Instance.Match.Live;</code></example>
    public LiveMatch? Live { get; private set; }

    /// <summary>Conecta a partida pareada: carrega o catalogo e abre a sessao nova.</summary>
    /// <example><code>match.Connect(pairing.Match);</code></example>
    public void Connect(MatchId match)
    {
        _ = ConnectAsync(match);
    }

    private async Task ConnectAsync(MatchId match)
    {
        // O catalogo fica em cache por geracao de sessao: com a Home ja tendo carregado, isto volta na hora.
        AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded = await catalog.LoadAsync();
        if (!loaded.IsSuccess)
        {
            log.Error("match_catalog_unavailable", new LogField("match_id", match.Value), new LogField("kind", loaded.Failure?.Kind.ToString() ?? "refused"));
            return;
        }

        StartLive(match, loaded.Value);
    }

    private void StartLive(MatchId match, LoadedCatalog loadedCatalog)
    {
        Live?.Dispose();
        LiveMatch live = new LiveMatch(Connection, matchBase, match, loadedCatalog, clock, log);
        Live = live;
        // Antes de carregar: a cena le o estado da sessao quando sobe, entao
        // ele precisa ja estar la.
        MatchSession.Instance.Attach(live);
        live.Mirror.ViewReplaced += OnViewReplaced;
        live.Start();
    }

    /// <summary>
    /// Chega no inicio da partida e de novo a cada reconexao, com o estado
    /// atual. Precisa ser idempotente: recarregar a cena numa reconexao
    /// apagaria a partida que o jogador estava vendo.
    /// </summary>
    private void OnViewReplaced(ViewReplaced change)
    {
        if (SceneManager.GetActiveScene().name == MatchSceneName)
            return;

        SceneManager.LoadScene(MatchSceneName);
    }
}
