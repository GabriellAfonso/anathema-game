#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Por que a partida não está disponível.</summary>
    /// <example><code>retryButton.SetActive(client.State.Unavailable?.Kind != MatchUnavailableKind.MatchRefused);</code></example>
    public enum MatchUnavailableKind
    {
        /// <summary>O catálogo não carregou; nenhum socket foi aberto.</summary>
        CatalogUnavailable,

        /// <summary>O servidor recusou a partida (<c>match_denied</c> ou fechamento 44xx).</summary>
        MatchRefused,

        /// <summary>A conexão de partida desistiu por um motivo que não é de sessão.</summary>
        ConnectionGaveUp,
    }
}
