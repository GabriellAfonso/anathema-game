#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Marcador que o cliente põe no <c>ping</c> e o servidor ecoa no <c>pong</c>. O
    /// servidor não acrescenta nada ao eco, então é o único jeito de casar um pong com
    /// o seu ping e medir latência (backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md).
    /// </summary>
    /// <example>
    /// <code>
    /// PingMarker marker = new PingMarker(sentAtMs: (long)(clock.Now.Ticks / TimeSpan.TicksPerMillisecond), sequence: 1);
    /// </code>
    /// </example>
    internal sealed class PingMarker : IEquatable<PingMarker>
    {
        /// <summary>Nome do campo do instante de envio.</summary>
        /// <example><code>payload.ReadInteger(PingMarker.SentAtMsField);</code></example>
        public const string SentAtMsField = "sent_at_ms";

        /// <summary>Nome do campo do número de sequência.</summary>
        /// <example><code>payload.ReadInteger(PingMarker.SequenceField);</code></example>
        public const string SequenceField = "ping_seq";

        /// <summary>Cria o marcador.</summary>
        /// <example><code>PingMarker marker = new PingMarker(1726000000000, 7);</code></example>
        public PingMarker(long sentAtMs, long sequence)
        {
            SentAtMs = sentAtMs;
            Sequence = sequence;
        }

        /// <summary>Instante de envio em milissegundos do relógio monotônico.</summary>
        /// <example><code>long sentAt = marker.SentAtMs;</code></example>
        public long SentAtMs { get; }

        /// <summary>Número de sequência do ping.</summary>
        /// <example><code>long sequence = marker.Sequence;</code></example>
        public long Sequence { get; }

        /// <summary>Mesmo instante e mesma sequência.</summary>
        /// <example><code>bool mine = pong.Marker != null &amp;&amp; pong.Marker.Equals(sent);</code></example>
        public bool Equals(PingMarker? other) => other != null && SentAtMs == other.SentAtMs && Sequence == other.Sequence;

        /// <summary>Mesmo instante e mesma sequência.</summary>
        /// <example><code>bool mine = marker.Equals((object)other);</code></example>
        public override bool Equals(object? obj) => Equals(obj as PingMarker);

        /// <summary>Hash dos dois campos.</summary>
        /// <example><code>int hash = marker.GetHashCode();</code></example>
        public override int GetHashCode() => (SentAtMs.GetHashCode() * 397) ^ Sequence.GetHashCode();

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = marker.ToString(); // sent_at_ms=1 ping_seq=7</code></example>
        public override string ToString() => $"{SentAtMsField}={SentAtMs} {SequenceField}={Sequence}";
    }
}
