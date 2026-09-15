#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê o histórico de partidas do autenticado em <c>GET /game/matches/</c>, página a página
    /// (FR-037 a FR-039).
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;MatchHistoryPage, HistoryRefusal&gt; page = await history.ReadPageAsync(new HistoryPageRequest());
    /// </code>
    /// </example>
    public sealed class MatchHistory
    {
        private readonly AuthenticatedHttpClient client;
        private readonly IClientLog log;
        private readonly AccountRoutes routes;
        private readonly AccountResponseReader responses;

        /// <summary>Cria a leitura do histórico.</summary>
        /// <example><code>MatchHistory history = new MatchHistory(client, codec, log, routes);</code></example>
        internal MatchHistory(AuthenticatedHttpClient client, IProtocolCodec codec, IClientLog log, AccountRoutes routes)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client), "history client is null: expected the authenticated http client");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "history log is null: expected the client log");
            this.routes = routes ?? throw new ArgumentNullException(nameof(routes), "history routes is null: expected the account routes");
            responses = new AccountResponseReader(codec, log);
        }

        /// <summary>Lê uma página.</summary>
        /// <example><code>AccountCallOutcome&lt;MatchHistoryPage, HistoryRefusal&gt; page = await history.ReadPageAsync(new HistoryPageRequest(2));</code></example>
        public async Task<AccountCallOutcome<MatchHistoryPage, HistoryRefusal>> ReadPageAsync(HistoryPageRequest request)
        {
            AuthenticatedCallResult result = await client.SendAsync(AuthenticatedRequest.Get(routes.MatchesPage(request))).ConfigureAwait(false);
            if (result.Failure != null)
                return AccountCallOutcome<MatchHistoryPage, HistoryRefusal>.Failed(result.Failure);

            if (result.Status != 200)
                return AccountCallOutcome<MatchHistoryPage, HistoryRefusal>.Refused(Refusal(result, request));

            AccountCallOutcome<MatchHistoryPage, HistoryRefusal> page = responses.ReadSuccess<MatchHistoryPage, HistoryRefusal>(result, MatchHistoryPage.Read);
            if (page.IsSuccess)
                LogUnknownEndReasons(page.Value);

            return page;
        }

        private static HistoryRefusal Refusal(AuthenticatedCallResult result, HistoryPageRequest request)
        {
            if (result.Status != 404)
                return HistoryRefusal.FromUnrecognized(new UnrecognizedRefusal(result.Status, result.BodyText));

            return request.Page > 1 ? HistoryRefusal.PastTheEnd() : HistoryRefusal.NoProfile();
        }

        private void LogUnknownEndReasons(MatchHistoryPage page)
        {
            foreach (MatchHistoryRow row in page.Rows)
            {
                if (row.EndReason == MatchEndReason.Unknown)
                    log.Warning("history_end_reason_unknown", new LogField("match_id", row.Match.Value), new LogField("end_reason", row.EndReasonText));
            }
        }
    }
}
