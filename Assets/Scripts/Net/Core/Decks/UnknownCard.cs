#nullable enable

namespace Anathema.Net.Core
{
    /// <summary><c>unknown_card</c>: uma carta da lista não está no catálogo.</summary>
    /// <example><code>MarkMissing(unknown.Card);</code></example>
    public sealed class UnknownCard : DeckProblem
    {
        private UnknownCard(IPayloadReader problem)
            : base(problem.ReadText("message"))
        {
            Card = problem.ReadCardId("card_id");
        }

        /// <summary>A carta que o catálogo não tem.</summary>
        /// <example><code>CardId card = unknown.Card;</code></example>
        public CardId Card { get; }

        internal static DeckProblem Read(IPayloadReader problem) => new UnknownCard(problem);
    }
}
