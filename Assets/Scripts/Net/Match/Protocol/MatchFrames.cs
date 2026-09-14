#nullable enable
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// A união de frames do codec único do cliente: os genéricos, os da fila e os do socket de partida
    /// (<c>match_start</c>, <c>match_update</c>, <c>turn_warning</c>). Um codec só para conta e os dois
    /// sockets (specs/004-match-session/research.md, R3).
    /// </summary>
    /// <example>
    /// <code>
    /// IProtocolCodec codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log);
    /// </code>
    /// </example>
    public static class MatchFrames
    {
        /// <summary>Cria a união com todos os braços conhecidos.</summary>
        /// <example><code>DiscriminatedUnion&lt;ServerFrame&gt; frames = MatchFrames.CreateUnion();</code></example>
        public static DiscriminatedUnion<ServerFrame> CreateUnion()
        {
            return ConnectionFrames.CreateUnion()
                .Register(MatchStartFrame.TypeName, MatchStartFrame.Read)
                .Register(MatchUpdateFrame.TypeName, MatchUpdateFrame.Read)
                .Register(TurnWarningFrame.TypeName, TurnWarningFrame.Read);
        }
    }
}
