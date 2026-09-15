#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Entra na fila com um deck. Não há resposta de sucesso: entrou é mandou e não veio recusa. Mandar
    /// de novo substitui a entrada e vai para o fim da fila (contrato 011 do backend,
    /// <c>specs/011-deck-catalog-api/contracts/matchmaking_messages.md</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// SocketSendOutcome sent = await connection.SendAsync(new JoinQueueMessage(deck));
    /// </code>
    /// </example>
    internal sealed class JoinQueueMessage : IOutgoingMessage
    {
        /// <summary>Valor de <c>type</c> da mensagem.</summary>
        /// <example><code>bool isJoin = message.MessageType == JoinQueueMessage.TypeName;</code></example>
        public const string TypeName = "join_queue";

        /// <summary>Mensagem com o deck escolhido.</summary>
        /// <example><code>JoinQueueMessage join = new JoinQueueMessage(new DeckId(4));</code></example>
        public JoinQueueMessage(DeckId deck)
        {
            Deck = deck;
        }

        /// <summary>O deck com que o jogador entra.</summary>
        /// <example><code>DeckId deck = join.Deck;</code></example>
        public DeckId Deck { get; }

        /// <inheritdoc />
        public string MessageType => TypeName;

        /// <inheritdoc />
        public void WritePayload(IPayloadWriter writer)
        {
            writer.WriteDeckId("deck_id", Deck);
        }
    }
}
