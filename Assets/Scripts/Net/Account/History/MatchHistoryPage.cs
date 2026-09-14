#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Uma página do histórico. <see cref="HasNext"/> e <see cref="HasPrevious"/> dizem só se há
    /// página vizinha; a URL do servidor não é usada, porque o host dela é o que o servidor enxerga.
    /// </summary>
    /// <example>
    /// <code>
    /// nextButton.interactable = page.HasNext;
    /// </code>
    /// </example>
    public sealed class MatchHistoryPage
    {
        private MatchHistoryPage(IPayloadReader body)
        {
            Count = body.ReadInteger("count");
            HasNext = body.ReadOptionalText("next") != null;
            HasPrevious = body.ReadOptionalText("previous") != null;
            Rows = body.ReadObjectList("results").Select(MatchHistoryRow.Read).ToArray();
        }

        /// <summary>Total de partidas registradas, em todas as páginas.</summary>
        /// <example><code>long total = page.Count;</code></example>
        public long Count { get; }

        /// <summary>Há página seguinte.</summary>
        /// <example><code>bool more = page.HasNext;</code></example>
        public bool HasNext { get; }

        /// <summary>Há página anterior.</summary>
        /// <example><code>bool back = page.HasPrevious;</code></example>
        public bool HasPrevious { get; }

        /// <summary>Linhas, da mais recente para a mais antiga.</summary>
        /// <example><code>MatchHistoryRow latest = page.Rows[0];</code></example>
        public IReadOnlyList<MatchHistoryRow> Rows { get; }

        internal static MatchHistoryPage Read(IPayloadReader body) => new MatchHistoryPage(body);
    }
}
