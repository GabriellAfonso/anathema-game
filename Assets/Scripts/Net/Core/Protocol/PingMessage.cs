#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// <c>ping</c> do batimento de aplicação. Existe porque o ping/pong do próprio
    /// WebSocket é respondido pelo <c>ClientWebSocket</c> sem passar pela aplicação
    /// (backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md).
    /// </summary>
    /// <example>
    /// <code>
    /// await socket.SendTextAsync(codec.Encode(new PingMessage(new PingMarker(sentAtMs, 1))));
    /// </code>
    /// </example>
    public sealed class PingMessage : IOutgoingMessage
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool isPing = message.MessageType == PingMessage.TypeName;</code></example>
        public const string TypeName = "ping";

        /// <summary>Cria o ping; sem marcador o payload sai vazio.</summary>
        /// <example><code>PingMessage bare = new PingMessage(null);</code></example>
        public PingMessage(PingMarker? marker)
        {
            Marker = marker;
        }

        /// <summary>Marcador ecoado pelo servidor, ou nulo.</summary>
        /// <example><code>PingMarker? sent = ping.Marker;</code></example>
        public PingMarker? Marker { get; }

        /// <summary>Sempre <c>ping</c>.</summary>
        /// <example><code>string type = ping.MessageType;</code></example>
        public string MessageType => TypeName;

        /// <summary>Escreve <c>sent_at_ms</c> e <c>ping_seq</c> quando há marcador.</summary>
        /// <example><code>ping.WritePayload(writer);</code></example>
        public void WritePayload(IPayloadWriter writer)
        {
            if (Marker == null)
                return;

            writer.WriteInteger(PingMarker.SentAtMsField, Marker.SentAtMs);
            writer.WriteInteger(PingMarker.SequenceField, Marker.Sequence);
        }
    }
}
