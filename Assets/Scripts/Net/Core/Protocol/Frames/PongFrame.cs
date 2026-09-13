#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Resposta a um <c>ping</c>, com o payload ecoado. O servidor devolve <c>{}</c>
    /// quando o ping não tinha objeto, então o marcador pode faltar sem que o frame seja
    /// inválido (backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md).
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is PongFrame pong &amp;&amp; sent.Equals(pong.Marker)) latency.Record(sent);
    /// </code>
    /// </example>
    public sealed class PongFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>union.Register(PongFrame.TypeName, PongFrame.Read);</code></example>
        public const string TypeName = "pong";

        /// <summary>Cria o pong.</summary>
        /// <example><code>PongFrame pong = new PongFrame(null);</code></example>
        public PongFrame(PingMarker? marker)
            : base(TypeName)
        {
            Marker = marker;
        }

        /// <summary>Marcador ecoado, ou nulo se o eco não tem <c>sent_at_ms</c> e <c>ping_seq</c> inteiros.</summary>
        /// <example><code>PingMarker? echoed = pong.Marker;</code></example>
        public PingMarker? Marker { get; }

        /// <summary>Braço da união: nunca falha por causa do marcador.</summary>
        /// <example><code>PongFrame pong = PongFrame.Read(payload);</code></example>
        public static PongFrame Read(IPayloadReader payload)
        {
            return new PongFrame(TryReadMarker(payload));
        }

        private static PingMarker? TryReadMarker(IPayloadReader payload)
        {
            try
            {
                long? sentAtMs = payload.ReadOptionalInteger(PingMarker.SentAtMsField);
                long? sequence = payload.ReadOptionalInteger(PingMarker.SequenceField);
                return sentAtMs.HasValue && sequence.HasValue ? new PingMarker(sentAtMs.Value, sequence.Value) : null;
            }
            catch (PayloadShapeException)
            {
                // Eco de um ping que não foi mandado por este cliente: não é marcador nosso.
                return null;
            }
        }
    }
}
