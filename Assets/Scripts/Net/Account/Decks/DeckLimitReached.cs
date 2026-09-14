#nullable enable
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>O jogador já está no teto de decks (chave <c>deck_limit</c> do 400 da criação).</summary>
    /// <example><code>ShowError(limit.Messages[0]);</code></example>
    public sealed class DeckLimitReached : DeckRefusalReason
    {
        /// <summary>Cria o motivo com as mensagens do servidor.</summary>
        /// <example><code>DeckRefusalReason reason = new DeckLimitReached(body.ReadTextList("deck_limit"));</code></example>
        public DeckLimitReached(IReadOnlyList<string> messages)
        {
            Messages = messages;
        }

        /// <summary>Mensagens do servidor.</summary>
        /// <example><code>string first = limit.Messages[0];</code></example>
        public IReadOnlyList<string> Messages { get; }
    }
}
