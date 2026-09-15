#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Uma jogada do cliente para o socket de partida. As mensagens concretas só aceitam
    /// <see cref="CardInstanceId"/>: a cópia na partida, nunca a entrada do catálogo
    /// (<c>backend/specs/009-match-protocol/contracts/client_messages.md</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// PlayCommand play = new PlayUnitCommand(new CardInstanceId(12));
    /// string frame = codec.Encode(play);
    /// </code>
    /// </example>
    public abstract class PlayCommand : IOutgoingMessage
    {
        /// <summary>Guarda o <c>type</c> exato da mensagem.</summary>
        /// <example><code>protected PassCommand() : base("pass") { }</code></example>
        protected PlayCommand(string messageType)
        {
            if (string.IsNullOrWhiteSpace(messageType))
                throw new ArgumentException($"play command type is '{messageType}': expected a message type like play_unit", nameof(messageType));

            MessageType = messageType;
        }

        /// <summary>Valor exato de <c>type</c> no envelope.</summary>
        /// <example><code>string type = play.MessageType; // "play_unit"</code></example>
        public string MessageType { get; }

        /// <summary>Escreve os campos do <c>payload</c>; comando sem campo não escreve nada.</summary>
        /// <example><code>play.WritePayload(writer);</code></example>
        void IOutgoingMessage.WritePayload(IPayloadWriter writer) => Write(writer);

        internal abstract void Write(IPayloadWriter writer);

        /// <summary>O <c>type</c>, para log.</summary>
        /// <example><code>log.Warning("match_command_not_sent", new LogField("command", play.ToString()));</code></example>
        public override string ToString() => MessageType;
    }
}
