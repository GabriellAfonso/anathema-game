#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>O servidor respondeu. O corpo vem em texto, sem interpretação.</summary>
    /// <example>
    /// <code>
    /// HttpResponse response = new HttpResponse(401, "{\"detail\": \"...\"}");
    /// </code>
    /// </example>
    internal sealed class HttpResponse : HttpOutcome
    {
        /// <summary>Cria a resposta; status fora de 100..599 lança.</summary>
        /// <example><code>HttpOutcome ok = new HttpResponse(200, body);</code></example>
        public HttpResponse(int status, string body)
        {
            if (status < 100 || status > 599)
                throw new ArgumentOutOfRangeException(nameof(status), status, $"http status is {status}: expected 100 to 599");

            Status = status;
            Body = body ?? throw new ArgumentNullException(nameof(body), $"body of http {status} is null: expected text, empty when the server sent none");
        }

        /// <summary>Status HTTP.</summary>
        /// <example><code>bool unauthorized = response.Status == 401;</code></example>
        public int Status { get; }

        /// <summary>Corpo em texto UTF-8.</summary>
        /// <example><code>string body = response.Body;</code></example>
        public string Body { get; }

        /// <summary>Esta própria resposta.</summary>
        /// <example><code>HttpResponse? same = response.AsResponse;</code></example>
        public override HttpResponse? AsResponse => this;

        /// <summary>Sempre nulo.</summary>
        /// <example><code>TransportFailure? none = response.AsFailure;</code></example>
        public override TransportFailure? AsFailure => null;
    }
}
