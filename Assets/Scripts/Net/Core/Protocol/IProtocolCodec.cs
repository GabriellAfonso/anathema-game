#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Texto do protocolo de ida e de volta. A biblioteca de JSON fica atrás desta
    /// interface. <see cref="Decode"/> nunca lança, qualquer que seja o texto
    /// (specs/001-server-connection/contracts/protocol-codec.md).
    /// </summary>
    /// <example>
    /// <code>
    /// socket.TextReceived += text =>
    /// {
    ///     DecodeOutcome&lt;ServerFrame&gt; frame = codec.Decode(text);
    ///     if (frame.IsValid) Route(frame.Value);
    /// };
    /// </code>
    /// </example>
    public interface IProtocolCodec
    {
        /// <summary>Envelope <c>{"type": …, "payload": {…}}</c> em uma linha, sempre com <c>payload</c>.</summary>
        /// <example><code>string frame = codec.Encode(new PingMessage(null)); // {"type":"ping","payload":{}}</code></example>
        string Encode(IOutgoingMessage message);

        /// <summary>Frame tipado ou decodificação inválida; nunca exceção.</summary>
        /// <example><code>DecodeOutcome&lt;ServerFrame&gt; outcome = codec.Decode(text);</code></example>
        DecodeOutcome<ServerFrame> Decode(string frameText);

        /// <summary>Objeto JSON solto (corpo HTTP), escrito pelo delegate.</summary>
        /// <example><code>string body = codec.EncodeObject(w => { w.WriteText("username", user); w.WriteText("password", pass); });</code></example>
        string EncodeObject(Action<IPayloadWriter> writeFields);

        /// <summary>Objeto JSON solto (corpo HTTP) para leitura; texto que não é objeto vira falha.</summary>
        /// <example><code>DecodeOutcome&lt;IPayloadReader&gt; login = codec.DecodeObject(response.Body);</code></example>
        DecodeOutcome<IPayloadReader> DecodeObject(string jsonText);
    }
}
