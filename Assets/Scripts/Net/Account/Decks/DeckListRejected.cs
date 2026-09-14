#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>A lista de cartas foi recusada, com todos os problemas de uma vez, na ordem do servidor.</summary>
    /// <example><code>WrongDeckSize? size = rejected.Problems.OfType&lt;WrongDeckSize&gt;().FirstOrDefault();</code></example>
    public sealed class DeckListRejected : DeckRefusalReason
    {
        /// <summary>Cria o motivo com os problemas lidos.</summary>
        /// <example><code>DeckRefusalReason reason = new DeckListRejected(problems);</code></example>
        public DeckListRejected(IReadOnlyList<DeckProblem> problems)
        {
            Problems = problems;
        }

        /// <summary>Problemas da lista.</summary>
        /// <example><code>int count = rejected.Problems.Count;</code></example>
        public IReadOnlyList<DeckProblem> Problems { get; }
    }
}
