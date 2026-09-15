#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um frame do servidor já tipado. Hierarquia fechada por <c>type</c>: cada feature
    /// registra os seus braços em <see cref="GenericServerFrames.CreateUnion"/>, e o que
    /// ninguém registrou chega como <see cref="UnknownServerFrame"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// switch (frame)
    /// {
    ///     case PongFrame pong: heartbeat.RecordPong(pong.Marker); break;
    ///     case MessageRefusedFrame refused: presenter.ShowRefusal(refused.Code); break;
    /// }
    /// </code>
    /// </example>
    internal abstract class ServerFrame
    {
        /// <summary>Cria o frame com o valor exato de <c>type</c>.</summary>
        /// <example><code>protected MatchFoundFrame() : base("match_found") { }</code></example>
        protected ServerFrame(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException($"frame type is '{messageType}': expected the envelope type text", nameof(messageType));

            MessageType = messageType;
        }

        /// <summary>Valor exato de <c>type</c> no envelope.</summary>
        /// <example><code>log.Debug("frame_received", new LogField("type", frame.MessageType));</code></example>
        public string MessageType { get; }
    }
}
