#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O único caminho das rotas autenticadas (FR-025): pede um token válido, põe o
    /// <c>Bearer</c> e, num 401, renova uma vez e repete uma vez (FR-026). Se outra chamada já
    /// trocou o token, repete com o atual sem renovar. Nunca lança e nunca registra corpo.
    /// </summary>
    /// <example>
    /// <code>
    /// AuthenticatedCallResult result = await client.SendAsync(AuthenticatedRequest.Get(routes.Cards));
    /// </code>
    /// </example>
    internal sealed class AuthenticatedHttpClient
    {
        private readonly IHttpTransport http;
        private readonly AccountSession session;
        private readonly IAccessTokenSource tokens;

        /// <summary>Cria o cliente sobre o transporte, a sessão e a porta de token.</summary>
        /// <example><code>AuthenticatedHttpClient client = new AuthenticatedHttpClient(http, session, tokens);</code></example>
        public AuthenticatedHttpClient(IHttpTransport http, AccountSession session, IAccessTokenSource tokens)
        {
            this.http = http ?? throw new ArgumentNullException(nameof(http), "authenticated client transport is null: expected the http transport");
            this.session = session ?? throw new ArgumentNullException(nameof(session), "authenticated client session is null: expected the account session");
            this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens), "authenticated client tokens is null: expected the access token source");
        }

        /// <summary>Envia o pedido com o token, renovando e repetindo uma vez num 401.</summary>
        /// <example><code>AuthenticatedCallResult result = await client.SendAsync(request);</code></example>
        public async Task<AuthenticatedCallResult> SendAsync(AuthenticatedRequest request)
        {
            AccessTokenOutcome token = await tokens.GetValidAsync().ConfigureAwait(false);
            if (token.Kind != AccessTokenOutcomeKind.Valid)
                return AuthenticatedCallResult.Failed(ToFailure(token));

            HttpOutcome first = await SendWithAsync(request, token.Token!).ConfigureAwait(false);
            if (first.AsResponse?.Status != 401)
                return ToResult(first);

            return await RetryAfterUnauthorizedAsync(request, token.Token!).ConfigureAwait(false);
        }

        private async Task<AuthenticatedCallResult> RetryAfterUnauthorizedAsync(AuthenticatedRequest request, AccessToken rejected)
        {
            AccessTokenOutcome replacement = await ReplacementForAsync(rejected).ConfigureAwait(false);
            if (replacement.Kind != AccessTokenOutcomeKind.Valid)
                return AuthenticatedCallResult.Failed(ToFailure(replacement));

            return ToResult(await SendWithAsync(request, replacement.Token!).ConfigureAwait(false));
        }

        private async Task<AccessTokenOutcome> ReplacementForAsync(AccessToken rejected)
        {
            AccessToken? current = session.CurrentTokens?.Access;
            if (current == null)
                return AccessTokenOutcome.SessionUnavailable(session.UnavailableKind);

            if (!current.HasSameTextAs(rejected))
                return AccessTokenOutcome.Valid(current);

            RenewalOutcome renewed = await tokens.RenewNowAsync().ConfigureAwait(false);
            return AccessTokenOutcome.FromRenewal(renewed);
        }

        private Task<HttpOutcome> SendWithAsync(AuthenticatedRequest request, AccessToken token)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer " + token.RevealForRequest(),
                ["Accept"] = "application/json",
            };
            if (request.JsonBody != null)
                headers["Content-Type"] = "application/json";

            return http.SendAsync(new HttpRequestSpec(request.Method, request.Url, headers, request.JsonBody));
        }

        private static AuthenticatedCallResult ToResult(HttpOutcome outcome)
        {
            if (outcome.AsFailure is TransportFailure failure)
                return AuthenticatedCallResult.Failed(AccountCallFailure.TransportFailed(failure));

            return AuthenticatedCallResult.Response(outcome.AsResponse!.Status, outcome.AsResponse.Body);
        }

        private static AccountCallFailure ToFailure(AccessTokenOutcome token)
        {
            if (token.Kind == AccessTokenOutcomeKind.SessionUnavailable)
                return AccountCallFailure.SessionUnavailable(token.Session!.Value);

            RenewalOutcome renewal = token.Renewal!;
            return renewal.Transport != null ? AccountCallFailure.TransportFailed(renewal.Transport) : AccountCallFailure.RenewalUnavailable(renewal);
        }
    }
}
