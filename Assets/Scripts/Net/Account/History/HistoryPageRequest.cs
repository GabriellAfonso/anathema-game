#nullable enable
using System;
using System.Globalization;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Qual página do histórico pedir. Página e tamanho menores que 1 não são pedido possível;
    /// tamanho acima de 100 vai como veio, e o servidor limita (contrato 012).
    /// </summary>
    /// <example>
    /// <code>
    /// HistoryPageRequest second = new HistoryPageRequest(page: 2, pageSize: 20);
    /// </code>
    /// </example>
    public sealed class HistoryPageRequest
    {
        /// <summary>Tamanho padrão do servidor.</summary>
        /// <example><code>int size = HistoryPageRequest.DefaultPageSize;</code></example>
        public const int DefaultPageSize = 20;

        /// <summary>Cria o pedido; valores menores que 1 lançam.</summary>
        /// <example><code>HistoryPageRequest first = new HistoryPageRequest();</code></example>
        public HistoryPageRequest(int page = 1, int pageSize = DefaultPageSize)
        {
            Page = RequirePositive(page, nameof(page));
            PageSize = RequirePositive(pageSize, "page_size");
        }

        /// <summary>Número da página, a partir de 1.</summary>
        /// <example><code>bool first = request.Page == 1;</code></example>
        public int Page { get; }

        /// <summary>Linhas por página.</summary>
        /// <example><code>int size = request.PageSize;</code></example>
        public int PageSize { get; }

        /// <summary>Texto de consulta da URL.</summary>
        /// <example><code>string query = request.ToQuery(); // ?page=2&amp;page_size=20</code></example>
        public string ToQuery()
        {
            return "?page=" + Page.ToString(CultureInfo.InvariantCulture) + "&page_size=" + PageSize.ToString(CultureInfo.InvariantCulture);
        }

        private static int RequirePositive(int value, string name)
        {
            if (value >= 1)
                return value;

            throw new ArgumentOutOfRangeException(name, value, $"{name} is {value}: expected an integer of at least 1");
        }
    }
}
