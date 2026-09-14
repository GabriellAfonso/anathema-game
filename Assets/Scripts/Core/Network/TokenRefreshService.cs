#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;

public enum TokenRefreshResult
{
    /// <summary>Token novo gravado na sessao.</summary>
    Success,

    /// <summary>Refresh recusado: a sessao morreu, o jogador precisa logar de novo.</summary>
    Expired,

    /// <summary>Servidor fora do ar ou sem rede. Vale tentar de novo depois.</summary>
    NetworkError,
}

/// <summary>
/// Troca o refresh token por um access token novo em /accounts/token/refresh/.
///
/// O access token do SimpleJWT dura 5 minutos por padrao, e o middleware de
/// WebSocket valida o token so no handshake. Um socket aberto sobrevive, mas
/// qualquer reconexao depois desses 5 minutos leva close 4001 sem isto aqui.
///
/// Desde a feature 002 e so uma ponte: delega para a renovacao da sessao de conta
/// (IAccessTokenSource.RenewNowAsync), que ja faz o pedido, guarda o token e decide
/// quando a sessao expira. Sai quando a feature 3 religar o BaseClient nas portas novas
/// (specs/002-player-account/plan.md, Complexity Tracking).
/// </summary>
/// <example><code>TokenRefreshService.Instance.Refresh(result => { if (result == TokenRefreshResult.Success) OpenSocket(); });</code></example>
public class TokenRefreshService
{
    private static TokenRefreshService? instance;

    /// <summary>
    /// Cria o servico sob demanda. Quem chama isto e o caminho de reconexao, que so roda
    /// quando algo ja deu errado; depender de alguem montar o servico antes daria
    /// NullReferenceException no pior momento possivel.
    /// </summary>
    /// <example><code>TokenRefreshService.Instance.Refresh(OnRefreshed);</code></example>
    public static TokenRefreshService Instance => instance ??= new TokenRefreshService();

    /// <summary>
    /// Pede um access token novo, sem olhar a margem: o BaseClient so chama isto depois de um 4001.
    /// Os tres clients podem tomar 4001 no mesmo frame; a renovacao da sessao e unica, entao as
    /// chamadas concorrentes compartilham a mesma requisicao e recebem o mesmo resultado.
    /// </summary>
    /// <example><code>TokenRefreshService.Instance.Refresh(OnRefreshed);</code></example>
    public void Refresh(Action<TokenRefreshResult> onDone)
    {
        _ = RefreshAsync(onDone);
    }

    private static async Task RefreshAsync(Action<TokenRefreshResult> onDone)
    {
        try
        {
            // Sem ConfigureAwait: o callback do BaseClient continua na thread principal.
            RenewalOutcome renewed = await PlayerSession.Instance.Account.Tokens.RenewNowAsync();
            onDone?.Invoke(ToResult(renewed));
        }
        catch (Exception unexpected)
        {
            PlayerSession.Instance.Log.Error("token_refresh_bridge_failed", new LogField("error", unexpected.GetType().Name));
            onDone?.Invoke(TokenRefreshResult.NetworkError);
        }
    }

    private static TokenRefreshResult ToResult(RenewalOutcome renewed)
    {
        // 401 e a unica resposta que mata a sessao; falha de rede nao reautentica o jogador.
        if (renewed.Kind == RenewalOutcomeKind.Renewed)
            return TokenRefreshResult.Success;

        return renewed.Kind == RenewalOutcomeKind.Unavailable ? TokenRefreshResult.NetworkError : TokenRefreshResult.Expired;
    }
}
