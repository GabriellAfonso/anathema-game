#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um problema da lista de cartas de um deck, como o servidor descreve em <c>deck_problems</c>.
    /// Mora no núcleo e não depende de HTTP: a recusa de deck pelo HTTP e a recusa
    /// <c>invalid_deck</c> do socket de fila leem a mesma forma (FR-035). O cliente decide por tipo,
    /// nunca por <see cref="Message"/>, que é para o jogador ler.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (DeckProblem problem in rejected.Problems) ShowLine(problem.Message);
    /// </code>
    /// </example>
    public abstract class DeckProblem
    {
        private protected DeckProblem(string message)
        {
            Message = message;
        }

        /// <summary>Texto do servidor, o mesmo no HTTP e no socket.</summary>
        /// <example><code>string line = problem.Message;</code></example>
        public string Message { get; }
    }
}
