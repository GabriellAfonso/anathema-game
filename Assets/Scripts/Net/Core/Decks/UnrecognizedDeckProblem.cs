#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Problema com <c>kind</c> que o cliente ainda não conhece. O contrato garante que um
    /// <c>kind</c> novo não é quebra se o cliente mostrar a <see cref="DeckProblem.Message"/>.
    /// </summary>
    /// <example><code>ShowLine(unrecognized.Message); // kind: banned_card</code></example>
    public sealed class UnrecognizedDeckProblem : DeckProblem
    {
        private UnrecognizedDeckProblem(string kind, IPayloadReader problem)
            : base(problem.ReadText("message"))
        {
            Kind = kind;
        }

        /// <summary>O texto de <c>kind</c> como veio.</summary>
        /// <example><code>string kind = unrecognized.Kind;</code></example>
        public string Kind { get; }

        internal static DeckProblem Read(string kind, IPayloadReader problem) => new UnrecognizedDeckProblem(kind, problem);
    }
}
