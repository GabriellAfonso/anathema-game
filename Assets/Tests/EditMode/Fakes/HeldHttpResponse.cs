#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Resposta HTTP que fica pendente até o teste soltar. Existe para provar concorrência sem
    /// thread nem espera: dez chamadas encontram a mesma renovação em curso, e só então o teste
    /// solta a resposta (specs/002-player-account/research.md, R2).
    /// </summary>
    /// <example>
    /// <code>
    /// HeldHttpResponse refresh = http.HoldNext();
    /// Task&lt;AccessTokenOutcome&gt; first = tokens.GetValidAsync();
    /// refresh.Release(200, "{\"access\": \"...\"}");
    /// </code>
    /// </example>
    public sealed class HeldHttpResponse
    {
        // Sem RunContinuationsAsynchronously: a continuação roda dentro de Release, na thread do
        // teste, como no adaptador real, que completa na thread principal.
        private readonly TaskCompletionSource<HttpOutcome> completion = new TaskCompletionSource<HttpOutcome>();

        /// <summary>Ainda não foi solta.</summary>
        /// <example><code>Assert.That(refresh.IsPending, Is.True);</code></example>
        public bool IsPending => !completion.Task.IsCompleted;

        internal Task<HttpOutcome> Outcome => completion.Task;

        /// <summary>Solta com uma resposta do servidor.</summary>
        /// <example><code>refresh.Release(401, "{}");</code></example>
        public void Release(int status, string body) => Complete(new HttpResponse(status, body));

        /// <summary>Solta com uma falha de transporte.</summary>
        /// <example><code>refresh.ReleaseFailure(TransportFailureKind.Timeout, "Request timeout");</code></example>
        public void ReleaseFailure(TransportFailureKind kind, string detail) => Complete(new TransportFailure(kind, detail));

        private void Complete(HttpOutcome outcome)
        {
            if (!completion.TrySetResult(outcome))
                throw new InvalidOperationException("held http response was already released: expected a single Release or ReleaseFailure per HoldNext");
        }
    }
}
