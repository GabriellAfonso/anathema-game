#nullable enable
using System;
using System.IO;
using Anathema.Net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anathema.Net.Json
{
    /// <summary>
    /// Codec do protocolo sobre Newtonsoft. Lê campo por campo, sem desserialização por
    /// reflexão, para o IL2CPP não ter o que remover (specs/001-server-connection/research.md, R7).
    /// Ordem das verificações em specs/001-server-connection/contracts/protocol-codec.md.
    /// </summary>
    /// <example>
    /// <code>
    /// IProtocolCodec codec = new NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log);
    /// DecodeOutcome&lt;ServerFrame&gt; frame = codec.Decode(text);
    /// </code>
    /// </example>
    public sealed class NewtonsoftProtocolCodec : IProtocolCodec
    {
        // Um match_update inteiro não passa de uma dúzia de níveis; 64 barra JSON hostil sem recursão funda.
        private const int MaxDepth = 64;
        private const string PayloadField = "payload";

        private readonly DiscriminatedUnion<ServerFrame> frames;
        private readonly IClientLog log;

        /// <summary>Cria o codec com a união de frames já registrada.</summary>
        /// <example><code>NewtonsoftProtocolCodec codec = new NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log);</code></example>
        public NewtonsoftProtocolCodec(DiscriminatedUnion<ServerFrame> frames, IClientLog log)
        {
            this.frames = frames ?? throw new ArgumentNullException(nameof(frames), "frame union is null: expected GenericServerFrames.CreateUnion() plus feature arms");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        }

        /// <summary>Envelope em uma linha, sempre com <c>payload</c>.</summary>
        /// <example><code>string frame = codec.Encode(new PingMessage(marker));</code></example>
        public string Encode(IOutgoingMessage message)
        {
            JObject payload = new JObject();
            message.WritePayload(new JObjectPayloadWriter(payload));
            JObject envelope = new JObject { [GenericServerFrames.TypeField] = message.MessageType, [PayloadField] = payload };
            return envelope.ToString(Formatting.None);
        }

        /// <summary>Frame tipado ou decodificação inválida; nenhuma exceção sai daqui.</summary>
        /// <example><code>DecodeOutcome&lt;ServerFrame&gt; outcome = codec.Decode(text);</code></example>
        public DecodeOutcome<ServerFrame> Decode(string frameText)
        {
            try
            {
                DecodeOutcome<IPayloadReader> envelope = DecodeObject(frameText);
                return envelope.IsValid ? DecodeEnvelope(envelope.Value) : DecodeOutcome<ServerFrame>.Invalid(envelope.Failure);
            }
            catch (Exception unexpected)
            {
                return UnexpectedFailure(unexpected, frameText);
            }
        }

        /// <summary>Objeto JSON solto, escrito pelo delegate.</summary>
        /// <example><code>string body = codec.EncodeObject(w => w.WriteText("username", "one"));</code></example>
        public string EncodeObject(Action<IPayloadWriter> writeFields)
        {
            JObject root = new JObject();
            writeFields(new JObjectPayloadWriter(root));
            return root.ToString(Formatting.None);
        }

        /// <summary>Objeto JSON solto para leitura; passos 1 e 2 do envelope.</summary>
        /// <example><code>IPayloadReader login = codec.DecodeObject(response.Body).Value;</code></example>
        public DecodeOutcome<IPayloadReader> DecodeObject(string jsonText)
        {
            if (jsonText == null)
                return Invalid<IPayloadReader>(DecodeFailureKind.NotJson, "", "frame is null: expected a JSON object");

            JToken? root = TryParse(jsonText, out string problem);
            if (root == null)
                return Invalid<IPayloadReader>(DecodeFailureKind.NotJson, "", $"{problem}; received {JsonSnippet.Of(jsonText)}: expected a JSON object");

            if (root.Type != JTokenType.Object)
                return Invalid<IPayloadReader>(DecodeFailureKind.NotObject, "", $"root is {JsonSnippet.Of(root)}: expected a JSON object");

            return DecodeOutcome<IPayloadReader>.Valid(new JObjectPayloadReader((JObject)root, ""));
        }

        private DecodeOutcome<ServerFrame> DecodeEnvelope(IPayloadReader envelope)
        {
            try
            {
                string type = DiscriminatorField.Read(envelope, GenericServerFrames.TypeField);
                return frames.DecodeBody(type, ReadPayload(envelope));
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<ServerFrame>.Invalid(shape.Failure);
            }
        }

        private static IPayloadReader ReadPayload(IPayloadReader envelope)
        {
            try
            {
                return envelope.ReadOptionalObject(PayloadField) ?? new JObjectPayloadReader(new JObject(), PayloadField);
            }
            catch (PayloadShapeException wrongType)
            {
                throw new PayloadShapeException(new DecodeFailure(DecodeFailureKind.PayloadNotObject, PayloadField, wrongType.Failure.Detail));
            }
        }

        private static JToken? TryParse(string text, out string problem)
        {
            try
            {
                using JsonTextReader reader = new JsonTextReader(new StringReader(text)) { MaxDepth = MaxDepth, DateParseHandling = DateParseHandling.None };
                JToken root = JToken.ReadFrom(reader);
                problem = reader.Read() ? $"extra content after the JSON value at position {reader.LinePosition}" : "";
                return problem.Length == 0 ? root : null;
            }
            catch (JsonException parseError)
            {
                problem = parseError.Message;
                return null;
            }
        }

        private DecodeOutcome<ServerFrame> UnexpectedFailure(Exception unexpected, string frameText)
        {
            string detail = $"{unexpected.GetType().Name}: {unexpected.Message}; received {JsonSnippet.Of(frameText ?? "null")}";
            log.Error("codec_unexpected_failure", new LogField("detail", detail));
            return Invalid<ServerFrame>(DecodeFailureKind.InvalidValue, "", detail);
        }

        private static DecodeOutcome<T> Invalid<T>(DecodeFailureKind kind, string path, string detail)
        {
            return DecodeOutcome<T>.Invalid(new DecodeFailure(kind, path, detail));
        }
    }
}
