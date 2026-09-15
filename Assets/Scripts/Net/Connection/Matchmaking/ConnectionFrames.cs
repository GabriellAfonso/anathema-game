#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// A união de frames que o codec da camada usa: os genéricos do núcleo (recusa, gates, pong) e os
    /// do socket de fila. Um codec só atende os dois sockets e a conta
    /// (specs/003-authenticated-socket-queue/research.md, R1).
    /// </summary>
    /// <example>
    /// <code>
    /// IProtocolCodec codec = new NewtonsoftProtocolCodec(ConnectionFrames.CreateUnion(), log);
    /// </code>
    /// </example>
    internal static class ConnectionFrames
    {
        /// <summary>União nova, com todos os braços conhecidos pela camada.</summary>
        /// <example><code>DiscriminatedUnion&lt;ServerFrame&gt; frames = ConnectionFrames.CreateUnion();</code></example>
        public static DiscriminatedUnion<ServerFrame> CreateUnion()
        {
            return GenericServerFrames.CreateUnion()
                .Register(MatchFoundFrame.TypeName, MatchFoundFrame.Read)
                .Register(MatchmakingFailedFrame.TypeName, MatchmakingFailedFrame.Read);
        }
    }
}
