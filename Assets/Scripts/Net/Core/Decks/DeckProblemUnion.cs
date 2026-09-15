#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// União fechada dos itens de <c>deck_problems</c> por <c>kind</c>, com os três braços dos
    /// contratos (<c>backend/specs/011-deck-catalog-api/contracts/http_decks.md</c> e
    /// <c>matchmaking_messages.md</c>) e um braço para <c>kind</c> novo que ainda lê a
    /// <c>message</c>. A feature 3 reusa esta união na recusa <c>invalid_deck</c> do socket de fila.
    /// </summary>
    /// <example>
    /// <code>
    /// DiscriminatedUnion&lt;DeckProblem&gt; problems = DeckProblemUnion.Create();
    /// DeckProblem[] listed = body.ReadObjectList("deck_problems").Select(problems.ReadNested).ToArray();
    /// </code>
    /// </example>
    internal static class DeckProblemUnion
    {
        /// <summary>Uma união nova com os braços registrados.</summary>
        /// <example><code>DiscriminatedUnion&lt;DeckProblem&gt; problems = DeckProblemUnion.Create();</code></example>
        public static DiscriminatedUnion<DeckProblem> Create()
        {
            return new DiscriminatedUnion<DeckProblem>("kind", UnrecognizedDeckProblem.Read)
                .Register("wrong_deck_size", WrongDeckSize.Read)
                .Register("too_many_copies", TooManyCopies.Read)
                .Register("unknown_card", UnknownCard.Read);
        }
    }
}
