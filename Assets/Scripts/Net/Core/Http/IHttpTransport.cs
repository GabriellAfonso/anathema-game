#nullable enable
using System.Threading.Tasks;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Transporte HTTP. Pode ser chamado de qualquer thread; no adaptador real a
    /// tarefa completa na thread principal. Nunca lança por status nem por falha
    /// de rede: tudo vira <see cref="HttpOutcome"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// HttpOutcome outcome = await http.SendAsync(new HttpRequestSpec("GET", cardsUrl));
    /// </code>
    /// </example>
    internal interface IHttpTransport
    {
        /// <summary>Envia o pedido e devolve resposta ou falha de transporte.</summary>
        /// <example><code>HttpOutcome outcome = await http.SendAsync(request);</code></example>
        Task<HttpOutcome> SendAsync(HttpRequestSpec request);
    }
}
