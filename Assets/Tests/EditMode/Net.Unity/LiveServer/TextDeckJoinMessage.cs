#nullable enable
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// <c>join_queue</c> com o <c>deck_id</c> em texto. O cliente de verdade nunca manda isso (o tipo
    /// <see cref="DeckId"/> impede); existe só para provar a recusa <c>deck_not_specified</c> real.
    /// </summary>
    internal sealed class TextDeckJoinMessage : IOutgoingMessage
    {
        private readonly string deckText;

        internal TextDeckJoinMessage(string deckText)
        {
            this.deckText = deckText;
        }

        public string MessageType => JoinQueueMessage.TypeName;

        public void WritePayload(IPayloadWriter writer) => writer.WriteText("deck_id", deckText);
    }
}
