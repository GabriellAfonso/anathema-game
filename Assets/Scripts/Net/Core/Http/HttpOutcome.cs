#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Resultado de um pedido HTTP: ou o servidor respondeu (qualquer status,
    /// inclusive 4xx/5xx), ou o transporte falhou antes disso. Os dois casos são
    /// mutuamente exclusivos e nenhum é exceção (FR-011, FR-012).
    /// </summary>
    /// <example>
    /// <code>
    /// HttpOutcome outcome = await http.SendAsync(request);
    /// if (outcome.AsResponse is HttpResponse response &amp;&amp; response.Status == 401) AskLogin();
    /// if (outcome.AsFailure is TransportFailure failure) ShowOffline(failure.Kind);
    /// </code>
    /// </example>
    public abstract class HttpOutcome
    {
        // private protected: só HttpResponse e TransportFailure, neste assembly, estendem.
        private protected HttpOutcome()
        {
        }

        /// <summary>A resposta do servidor, ou nulo se o transporte falhou.</summary>
        /// <example><code>HttpResponse? response = outcome.AsResponse;</code></example>
        public abstract HttpResponse? AsResponse { get; }

        /// <summary>A falha de transporte, ou nulo se o servidor respondeu.</summary>
        /// <example><code>TransportFailure? failure = outcome.AsFailure;</code></example>
        public abstract TransportFailure? AsFailure { get; }
    }
}
