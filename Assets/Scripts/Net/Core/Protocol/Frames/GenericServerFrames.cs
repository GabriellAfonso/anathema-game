#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// A união de frames do servidor com os braços que valem em qualquer socket:
    /// <c>message_refused</c>, <c>auth_denied</c> e <c>pong</c>. As features seguintes
    /// registram os seus antes de construir o codec.
    /// </summary>
    /// <example>
    /// <code>
    /// DiscriminatedUnion&lt;ServerFrame&gt; frames = GenericServerFrames.CreateUnion();
    /// IProtocolCodec codec = new NewtonsoftProtocolCodec(frames, log);
    /// </code>
    /// </example>
    public static class GenericServerFrames
    {
        /// <summary>Campo discriminador do envelope.</summary>
        /// <example><code>string field = GenericServerFrames.TypeField; // "type"</code></example>
        public const string TypeField = "type";

        /// <summary>União nova, com os três braços genéricos registrados.</summary>
        /// <example><code>DiscriminatedUnion&lt;ServerFrame&gt; frames = GenericServerFrames.CreateUnion();</code></example>
        public static DiscriminatedUnion<ServerFrame> CreateUnion()
        {
            return new DiscriminatedUnion<ServerFrame>(TypeField, type => new UnknownServerFrame(type))
                .Register(MessageRefusedFrame.TypeName, MessageRefusedFrame.Read)
                .Register(AuthDeniedFrame.TypeName, AuthDeniedFrame.Read)
                .Register(PongFrame.TypeName, PongFrame.Read);
        }
    }
}
