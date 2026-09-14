#nullable enable

namespace Anathema.Net.Core
{
    /// <summary><c>too_many_copies</c>: uma carta passou do limite de cópias.</summary>
    /// <example><code>MarkCard(copies.Card, copies.Count, copies.Limit);</code></example>
    public sealed class TooManyCopies : DeckProblem
    {
        private TooManyCopies(IPayloadReader problem)
            : base(problem.ReadText("message"))
        {
            Card = problem.ReadCardId("card_id");
            Count = problem.ReadInteger("count");
            Limit = problem.ReadInteger("limit");
        }

        /// <summary>A carta repetida.</summary>
        /// <example><code>CardId card = copies.Card;</code></example>
        public CardId Card { get; }

        /// <summary>Quantas cópias vieram.</summary>
        /// <example><code>long count = copies.Count;</code></example>
        public long Count { get; }

        /// <summary>Limite de cópias.</summary>
        /// <example><code>long limit = copies.Limit;</code></example>
        public long Limit { get; }

        internal static DeckProblem Read(IPayloadReader problem) => new TooManyCopies(problem);
    }
}
