#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Anathema.Net.Core;
using UnityEngine.Networking;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// <see cref="IHttpTransport"/> sobre <c>UnityWebRequest</c>. O pedido é criado dentro da
    /// <see cref="MainThreadQueue"/>, porque o <c>UnityWebRequest</c> só roda na thread
    /// principal; a tarefa completa lá também. Status 4xx/5xx é resposta; só falha de
    /// transporte vira <see cref="TransportFailure"/> (specs/001-server-connection/research.md, R5).
    /// </summary>
    /// <example>
    /// <code>
    /// IHttpTransport http = new UnityHttpTransport(queue, policy);
    /// HttpOutcome outcome = await http.SendAsync(new HttpRequestSpec("GET", new Uri(httpBase + "/game/cards/")));
    /// </code>
    /// </example>
    public sealed class UnityHttpTransport : IHttpTransport
    {
        private readonly MainThreadQueue queue;
        private readonly CleartextPolicy policy;

        /// <summary>Cria o transporte.</summary>
        /// <example><code>IHttpTransport http = new UnityHttpTransport(queue, new CleartextPolicy(Debug.isDebugBuild));</code></example>
        public UnityHttpTransport(MainThreadQueue queue, CleartextPolicy policy)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy), "policy is null: expected the cleartext policy of this build");
        }

        /// <summary>Envia de qualquer thread; o resultado sai na thread principal.</summary>
        /// <example><code>HttpOutcome outcome = await http.SendAsync(request);</code></example>
        public Task<HttpOutcome> SendAsync(HttpRequestSpec request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "http request is null: expected an HttpRequestSpec");

            TaskCompletionSource<HttpOutcome> completion = new TaskCompletionSource<HttpOutcome>();
            queue.Enqueue(() => Start(request, completion));
            return completion.Task;
        }

        private void Start(HttpRequestSpec request, TaskCompletionSource<HttpOutcome> completion)
        {
            if (!policy.Permits(request.Url))
            {
                completion.SetResult(CleartextRefused(request.Url.GetLeftPart(UriPartial.Path)));
                return;
            }

            UnityWebRequest web = Build(request);
            try
            {
                web.SendWebRequest().completed += _ => Finish(web, completion);
            }
            catch (InvalidOperationException insecure)
            {
                // insecureHttpOption do Unity recusou http:// neste build.
                web.Dispose();
                completion.SetResult(CleartextRefused(insecure.Message));
            }
        }

        private static UnityWebRequest Build(HttpRequestSpec request)
        {
            UnityWebRequest web = new UnityWebRequest(request.Url, request.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = request.TimeoutSeconds,
            };
            if (request.Body != null)
                web.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.Body));

            foreach (KeyValuePair<string, string> header in request.Headers)
                web.SetRequestHeader(header.Key, header.Value);

            return web;
        }

        private static void Finish(UnityWebRequest web, TaskCompletionSource<HttpOutcome> completion)
        {
            try
            {
                completion.SetResult(ToOutcome(web));
            }
            finally
            {
                web.Dispose();
            }
        }

        private static HttpOutcome ToOutcome(UnityWebRequest web)
        {
            bool answered = web.result == UnityWebRequest.Result.Success || web.result == UnityWebRequest.Result.ProtocolError;
            if (answered && web.responseCode >= 100)
                return new HttpResponse((int)web.responseCode, web.downloadHandler?.text ?? string.Empty);

            string error = web.error ?? $"request ended with result {web.result} and status {web.responseCode}";
            return new TransportFailure(UnityWebRequestErrorClassifier.Classify(error), error);
        }

        private static HttpOutcome CleartextRefused(string detail)
        {
            return new TransportFailure(TransportFailureKind.CleartextRefused, $"{detail}: expected https in a build that does not allow cleartext");
        }
    }
}
