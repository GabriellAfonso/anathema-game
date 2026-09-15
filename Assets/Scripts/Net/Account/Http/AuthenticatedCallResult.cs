#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O que o cliente autenticado conseguiu: uma resposta do servidor (qualquer status) ou uma
    /// falha comum. Cada serviço transforma a resposta em sucesso ou recusa tipada.
    /// </summary>
    /// <example>
    /// <code>
    /// AuthenticatedCallResult result = await client.SendAsync(AuthenticatedRequest.Get(routes.Cards));
    /// if (result.Failure == null &amp;&amp; result.Status == 200) Read(result.BodyText);
    /// </code>
    /// </example>
    internal sealed class AuthenticatedCallResult
    {
        private AuthenticatedCallResult(int status, string bodyText, AccountCallFailure? failure)
        {
            Status = status;
            BodyText = bodyText;
            Failure = failure;
        }

        /// <summary>Status da resposta; 0 quando houve falha.</summary>
        /// <example><code>int status = result.Status;</code></example>
        public int Status { get; }

        /// <summary>Corpo da resposta; vazio quando houve falha.</summary>
        /// <example><code>string body = result.BodyText;</code></example>
        public string BodyText { get; }

        /// <summary>A falha comum, ou nulo quando o servidor respondeu.</summary>
        /// <example><code>AccountCallFailure? failure = result.Failure;</code></example>
        public AccountCallFailure? Failure { get; }

        /// <summary>O servidor respondeu.</summary>
        /// <example><code>return AuthenticatedCallResult.Response(404, body);</code></example>
        public static AuthenticatedCallResult Response(int status, string bodyText)
        {
            return new AuthenticatedCallResult(status, bodyText ?? throw new ArgumentNullException(nameof(bodyText), $"body of http {status} is null: expected text"), null);
        }

        /// <summary>Não houve resposta utilizável.</summary>
        /// <example><code>return AuthenticatedCallResult.Failed(AccountCallFailure.SessionUnavailable(SessionUnavailableKind.NoSession));</code></example>
        public static AuthenticatedCallResult Failed(AccountCallFailure failure)
        {
            return new AuthenticatedCallResult(0, string.Empty, failure ?? throw new ArgumentNullException(nameof(failure), "authenticated call failure is null: expected the common failure"));
        }
    }
}
