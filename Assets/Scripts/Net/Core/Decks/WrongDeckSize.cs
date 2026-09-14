#nullable enable

namespace Anathema.Net.Core
{
    /// <summary><c>wrong_deck_size</c>: o deck não tem o número exigido de cartas.</summary>
    /// <example><code>string hint = $"{size.Found}/{size.Required}";</code></example>
    public sealed class WrongDeckSize : DeckProblem
    {
        private WrongDeckSize(IPayloadReader problem)
            : base(problem.ReadText("message"))
        {
            Found = problem.ReadInteger("found");
            Required = problem.ReadInteger("required");
        }

        /// <summary>Cartas enviadas.</summary>
        /// <example><code>long found = size.Found;</code></example>
        public long Found { get; }

        /// <summary>Cartas exigidas.</summary>
        /// <example><code>long required = size.Required;</code></example>
        public long Required { get; }

        internal static DeckProblem Read(IPayloadReader problem) => new WrongDeckSize(problem);
    }
}
