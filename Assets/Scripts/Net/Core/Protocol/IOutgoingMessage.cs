#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Uma mensagem do cliente para o servidor: o <c>type</c> e os campos do <c>payload</c>.</summary>
    /// <example>
    /// <code>
    /// string frame = codec.Encode(new PingMessage(marker));
    /// </code>
    /// </example>
    public interface IOutgoingMessage
    {
        /// <summary>Valor exato de <c>type</c> no envelope.</summary>
        /// <example><code>string type = message.MessageType; // "ping"</code></example>
        string MessageType { get; }

        /// <summary>Escreve os campos do <c>payload</c>; não escrever nada gera <c>{}</c>.</summary>
        /// <example><code>message.WritePayload(writer);</code></example>
        void WritePayload(IPayloadWriter writer);
    }
}
