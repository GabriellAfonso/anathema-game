#nullable enable
using System;
using System.IO;
using System.Text;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Junta os fragmentos de uma mensagem WebSocket até o fim dela. Decodifica UTF-8 só
    /// no fim porque um caractere multibyte pode ser cortado entre dois fragmentos, e o
    /// estado inteiro da partida passa de qualquer buffer razoável (FR-009).
    /// </summary>
    /// <example>
    /// <code>
    /// assembler.Append(new ArraySegment&lt;byte&gt;(chunk, 0, result.Count));
    /// if (result.EndOfMessage) Deliver(assembler.Complete());
    /// </code>
    /// </example>
    internal sealed class WebSocketMessageAssembler
    {
        private readonly MemoryStream buffer = new MemoryStream();

        /// <summary>Acrescenta um fragmento.</summary>
        /// <example><code>assembler.Append(new ArraySegment&lt;byte&gt;(chunk, 0, count));</code></example>
        public void Append(ArraySegment<byte> fragment)
        {
            if (fragment.Array == null)
                throw new ArgumentException("fragment has no array: expected bytes received from the socket", nameof(fragment));

            buffer.Write(fragment.Array, fragment.Offset, fragment.Count);
        }

        /// <summary>Texto inteiro da mensagem; recomeça vazio.</summary>
        /// <example><code>string text = assembler.Complete();</code></example>
        public string Complete()
        {
            string text = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
            Reset();
            return text;
        }

        /// <summary>Descarta o que foi acumulado.</summary>
        /// <example><code>assembler.Reset();</code></example>
        public void Reset()
        {
            buffer.SetLength(0);
        }
    }
}
