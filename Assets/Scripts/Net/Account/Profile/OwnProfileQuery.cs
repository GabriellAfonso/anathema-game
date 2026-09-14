#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê o próprio perfil em <c>GET /players/me/</c> pelo cliente autenticado (FR-006). A rota
    /// não tem contrato escrito; a forma está na descrição da feature 002.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;OwnProfile, ProfileRefusal&gt; profile = await profileQuery.ReadAsync();
    /// </code>
    /// </example>
    public sealed class OwnProfileQuery
    {
        private readonly AuthenticatedHttpClient client;
        private readonly AccountResponseReader responses;
        private readonly AccountRoutes routes;

        /// <summary>Cria a consulta.</summary>
        /// <example><code>OwnProfileQuery profileQuery = new OwnProfileQuery(client, codec, log, routes);</code></example>
        public OwnProfileQuery(AuthenticatedHttpClient client, IProtocolCodec codec, IClientLog log, AccountRoutes routes)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client), "profile query client is null: expected the authenticated http client");
            this.routes = routes ?? throw new ArgumentNullException(nameof(routes), "profile query routes is null: expected the account routes");
            responses = new AccountResponseReader(codec, log);
        }

        /// <summary>Lê o perfil.</summary>
        /// <example><code>AccountCallOutcome&lt;OwnProfile, ProfileRefusal&gt; profile = await profileQuery.ReadAsync();</code></example>
        public async Task<AccountCallOutcome<OwnProfile, ProfileRefusal>> ReadAsync()
        {
            AuthenticatedCallResult result = await client.SendAsync(AuthenticatedRequest.Get(routes.OwnProfile)).ConfigureAwait(false);
            if (result.Failure != null)
                return AccountCallOutcome<OwnProfile, ProfileRefusal>.Failed(result.Failure);

            if (result.Status == 200)
                return responses.ReadSuccess<OwnProfile, ProfileRefusal>(result, OwnProfile.Read);

            ProfileRefusal refusal = result.Status == 404 ? ProfileRefusal.Missing() : ProfileRefusal.FromUnrecognized(new UnrecognizedRefusal(result.Status, result.BodyText));
            return AccountCallOutcome<OwnProfile, ProfileRefusal>.Refused(refusal);
        }
    }
}
