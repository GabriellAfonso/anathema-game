#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Newtonsoft.Json.Linq;

namespace Anathema.Net.Json
{
    /// <summary>
    /// <see cref="IPayloadReader"/> sobre um <c>JObject</c>. Construtor interno: só o
    /// codec cria, para que <c>JObject</c> não apareça fora deste assembly.
    /// </summary>
    /// <example>
    /// <code>
    /// IPayloadReader reader = codec.DecodeObject(response.Body).Value;
    /// string token = reader.ReadText("token");
    /// </code>
    /// </example>
    public sealed class JObjectPayloadReader : IPayloadReader
    {
        private readonly JObject source;

        internal JObjectPayloadReader(JObject source, string path)
        {
            this.source = source;
            Path = path;
        }

        /// <summary>Caminho deste objeto; vazio na raiz.</summary>
        /// <example><code>string where = reader.Path;</code></example>
        public string Path { get; }

        /// <summary>Nomes dos campos presentes.</summary>
        /// <example><code>IReadOnlyCollection&lt;string&gt; names = reader.FieldNames;</code></example>
        public IReadOnlyCollection<string> FieldNames => source.Properties().Select(property => property.Name).ToArray();

        /// <summary>O campo existe.</summary>
        /// <example><code>bool extra = reader.Has("deck_id");</code></example>
        public bool Has(string field) => source.ContainsKey(field);

        /// <summary>Texto obrigatório.</summary>
        /// <example><code>string code = reader.ReadText("code");</code></example>
        public string ReadText(string field) => AsText(field, Required(field));

        /// <summary>Inteiro obrigatório.</summary>
        /// <example><code>long deckId = reader.ReadInteger("deck_id");</code></example>
        public long ReadInteger(string field) => AsInteger(PayloadPath.Field(Path, field), field, Required(field));

        /// <summary>Booleano obrigatório.</summary>
        /// <example><code>bool taken = reader.ReadBoolean("mulligan_taken");</code></example>
        public bool ReadBoolean(string field)
        {
            JToken token = Required(field);
            RequireType(token, JTokenType.Boolean, field, "a boolean");
            return token.Value<bool>();
        }

        /// <summary>Objeto obrigatório.</summary>
        /// <example><code>IPayloadReader self = reader.ReadObject("self");</code></example>
        public IPayloadReader ReadObject(string field) => AsObject(field, Required(field));

        /// <summary>Lista obrigatória de objetos.</summary>
        /// <example><code>IReadOnlyList&lt;IPayloadReader&gt; problems = reader.ReadObjectList("deck_problems");</code></example>
        public IReadOnlyList<IPayloadReader> ReadObjectList(string field)
        {
            JArray items = AsArray(field, Required(field));
            string listPath = PayloadPath.Field(Path, field);
            return items.Select((item, index) => ItemAsObject(PayloadPath.Item(listPath, index), item)).ToArray();
        }

        /// <summary>Lista obrigatória de inteiros.</summary>
        /// <example><code>IReadOnlyList&lt;long&gt; ids = reader.ReadIntegerList("card_ids");</code></example>
        public IReadOnlyList<long> ReadIntegerList(string field)
        {
            JArray items = AsArray(field, Required(field));
            string listPath = PayloadPath.Field(Path, field);
            return items.Select((item, index) => AsInteger(PayloadPath.Item(listPath, index), field, item)).ToArray();
        }

        /// <summary>Lista obrigatória de textos; item que não é texto aponta o índice.</summary>
        /// <example><code>IReadOnlyList&lt;string&gt; messages = reader.ReadTextList("name");</code></example>
        public IReadOnlyList<string> ReadTextList(string field)
        {
            JArray items = AsArray(field, Required(field));
            string listPath = PayloadPath.Field(Path, field);
            return items.Select((item, index) => ItemAsText(PayloadPath.Item(listPath, index), item)).ToArray();
        }

        /// <summary>Texto opcional.</summary>
        /// <example><code>string? reason = reader.ReadOptionalText("reason");</code></example>
        public string? ReadOptionalText(string field)
        {
            JToken? token = Optional(field);
            return token == null ? null : AsText(field, token);
        }

        /// <summary>Inteiro opcional.</summary>
        /// <example><code>long? deckId = reader.ReadOptionalInteger("deck_id");</code></example>
        public long? ReadOptionalInteger(string field)
        {
            JToken? token = Optional(field);
            return token == null ? (long?)null : AsInteger(PayloadPath.Field(Path, field), field, token);
        }

        /// <summary>Objeto opcional.</summary>
        /// <example><code>IPayloadReader? outcome = reader.ReadOptionalObject("outcome");</code></example>
        public IPayloadReader? ReadOptionalObject(string field)
        {
            JToken? token = Optional(field);
            return token == null ? null : AsObject(field, token);
        }

        private JToken Required(string field)
        {
            if (source.TryGetValue(field, out JToken? token) && token != null)
                return token;

            throw Shape(DecodeFailureKind.MissingField, PayloadPath.Field(Path, field), $"{field} is missing: expected the field to be present");
        }

        private JToken? Optional(string field)
        {
            if (!source.TryGetValue(field, out JToken? token) || token == null)
                return null;

            return token.Type == JTokenType.Null ? null : token;
        }

        private string AsText(string field, JToken token)
        {
            RequireType(token, JTokenType.String, field, "text");
            return token.Value<string>() ?? string.Empty;
        }

        private IPayloadReader AsObject(string field, JToken token)
        {
            RequireType(token, JTokenType.Object, field, "an object");
            return new JObjectPayloadReader((JObject)token, PayloadPath.Field(Path, field));
        }

        private JArray AsArray(string field, JToken token)
        {
            RequireType(token, JTokenType.Array, field, "a list");
            return (JArray)token;
        }

        private static IPayloadReader ItemAsObject(string itemPath, JToken item)
        {
            if (item.Type != JTokenType.Object)
                throw Shape(DecodeFailureKind.WrongFieldType, itemPath, $"{itemPath} is {JsonSnippet.Of(item)}: expected an object");

            return new JObjectPayloadReader((JObject)item, itemPath);
        }

        private static string ItemAsText(string itemPath, JToken item)
        {
            if (item.Type != JTokenType.String)
                throw Shape(DecodeFailureKind.WrongFieldType, itemPath, $"{itemPath} is {JsonSnippet.Of(item)}: expected text");

            return item.Value<string>() ?? string.Empty;
        }

        private static long AsInteger(string path, string field, JToken token)
        {
            if (token.Type != JTokenType.Integer)
                throw Shape(DecodeFailureKind.WrongFieldType, path, $"{field} is {JsonSnippet.Of(token)}: expected an integer");

            // Inteiro fora de 64 bits chega como BigInteger, e Value<long>() lança InvalidCastException.
            if (token is JValue { Value: long value })
                return value;

            throw Shape(DecodeFailureKind.InvalidValue, path, $"{field} is {JsonSnippet.Of(token)}: expected an integer that fits in 64 bits");
        }

        private void RequireType(JToken token, JTokenType expected, string field, string expectedShape)
        {
            if (token.Type != expected)
                throw Shape(DecodeFailureKind.WrongFieldType, PayloadPath.Field(Path, field), $"{field} is {JsonSnippet.Of(token)}: expected {expectedShape}");
        }

        private static PayloadShapeException Shape(DecodeFailureKind kind, string path, string detail)
        {
            return new PayloadShapeException(new DecodeFailure(kind, path, detail));
        }
    }
}
