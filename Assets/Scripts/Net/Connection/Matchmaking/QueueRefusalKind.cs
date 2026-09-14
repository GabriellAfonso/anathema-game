#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Recusas do socket de fila, pelo <c>code</c> do servidor (FR-031).</summary>
    /// <example><code>if (refusal.Kind == QueueRefusalKind.InvalidDeck) ShowProblems(refusal.Problems);</code></example>
    public enum QueueRefusalKind
    {
        /// <summary><c>deck_not_specified</c>: sem <c>deck_id</c> inteiro.</summary>
        DeckNotSpecified,

        /// <summary><c>deck_not_found</c>: o deck não existe ou é de outro jogador.</summary>
        DeckNotFound,

        /// <summary><c>invalid_deck</c>: o deck não passa nas regras.</summary>
        InvalidDeck,

        /// <summary><c>malformed_message</c>.</summary>
        MalformedMessage,

        /// <summary><c>unknown_message_type</c>.</summary>
        UnknownMessageType,

        /// <summary>Código que o cliente não conhece, ou código conhecido com forma quebrada.</summary>
        Unrecognized,
    }
}
