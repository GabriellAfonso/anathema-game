#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Transporte HTTP roteirizado: cada pedido consome o próximo resultado da fila.
    /// Pedido sem roteiro lança com método e URL, para o teste dizer o que faltou.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeHttpTransport http = new FakeHttpTransport();
    /// http.RespondNext(401, "{}");
    /// HttpOutcome outcome = http.SendAsync(request).Result;
    /// </code>
    /// </example>
    public sealed class FakeHttpTransport : IHttpTransport
    {
        private readonly Queue<HttpOutcome> scripted = new Queue<HttpOutcome>();
        private readonly List<HttpRequestSpec> requests = new List<HttpRequestSpec>();

        /// <summary>Pedidos recebidos, em ordem.</summary>
        /// <example><code>HttpRequestSpec first = http.Requests[0];</code></example>
        public IReadOnlyList<HttpRequestSpec> Requests => requests;

        /// <summary>Roteiriza uma resposta do servidor.</summary>
        /// <example><code>http.RespondNext(200, "{\"token\": \"abc\"}");</code></example>
        public void RespondNext(int status, string body)
        {
            scripted.Enqueue(new HttpResponse(status, body));
        }

        /// <summary>Roteiriza uma falha de transporte.</summary>
        /// <example><code>http.FailNext(TransportFailureKind.Timeout, "Request timeout");</code></example>
        public void FailNext(TransportFailureKind kind, string detail)
        {
            scripted.Enqueue(new TransportFailure(kind, detail));
        }

        /// <summary>Registra o pedido e devolve o próximo resultado roteirizado.</summary>
        /// <example><code>HttpOutcome outcome = await http.SendAsync(request);</code></example>
        public Task<HttpOutcome> SendAsync(HttpRequestSpec request)
        {
            requests.Add(request);
            if (scripted.Count == 0)
                throw new InvalidOperationException($"FakeHttpTransport got {request.Method} {request.Url} with nothing scripted: call RespondNext or FailNext first");

            return Task.FromResult(scripted.Dequeue());
        }
    }
}
