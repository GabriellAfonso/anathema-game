#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Um pedido a <c>/accounts/token/refresh/</c> e o mapeamento da resposta (research R5): 200
    /// com <c>access</c> legível renova; 401 expira (o <c>TokenRefreshView</c> responde 401 para
    /// refresh vencido, malformado ou de usuário inativo); o resto fica indisponível. Usado pela
    /// renovação e pela retomada, para as duas lerem a resposta do mesmo jeito.
    /// </summary>
    /// <example>
    /// <code>
    /// RenewalOutcome outcome = await refreshCall.SendAsync(stored);
    /// </code>
    /// </example>
    internal sealed class RefreshCall
    {
        private readonly IHttpTransport http;
        private readonly IProtocolCodec codec;
        private readonly IMonotonicClock clock;
        private readonly AccountRoutes routes;
        private readonly AccessTokenReader reader;

        /// <summary>Cria a chamada sobre as portas da sessão.</summary>
        /// <example><code>RefreshCall call = new RefreshCall(http, codec, clock, routes);</code></example>
        internal RefreshCall(IHttpTransport http, IProtocolCodec codec, IMonotonicClock clock, AccountRoutes routes)
        {
            this.http = http;
            this.codec = codec;
            this.clock = clock;
            this.routes = routes;
            reader = new AccessTokenReader(codec);
        }

        /// <summary>Troca o refresh por um acesso novo.</summary>
        /// <example><code>RenewalOutcome outcome = await call.SendAsync(tokens.Refresh);</code></example>
        internal async Task<RenewalOutcome> SendAsync(RefreshToken refresh)
        {
            string body = codec.EncodeObject(writer => AccountRequestBodies.WriteRefresh(writer, refresh));
            HttpOutcome outcome = await http.SendAsync(AccountRequestBodies.JsonPost(routes.Refresh, body)).ConfigureAwait(false);
            return Map(outcome);
        }

        private RenewalOutcome Map(HttpOutcome outcome)
        {
            if (outcome.AsFailure is TransportFailure failure)
                return RenewalOutcome.TransportFailed(failure);

            HttpResponse response = outcome.AsResponse!;
            if (response.Status == 401)
                return RenewalOutcome.SessionExpired();

            if (response.Status != 200)
                return RenewalOutcome.ServerStatus(response.Status);

            DecodeOutcome<AccessToken> access = AccountRequestBodies.ReadRefreshedAccess(codec, reader, response.Body, clock.Now);
            return access.IsValid ? RenewalOutcome.Renewed(access.Value) : RenewalOutcome.OutOfContract(access.Failure);
        }
    }
}
