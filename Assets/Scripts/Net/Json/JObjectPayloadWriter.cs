#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Newtonsoft.Json.Linq;

namespace Anathema.Net.Json
{
    /// <summary>
    /// <see cref="IPayloadWriter"/> que monta um <c>JObject</c>. Construtor interno: só o
    /// codec cria.
    /// </summary>
    /// <example>
    /// <code>
    /// string body = codec.EncodeObject(writer => writer.WriteText("username", username));
    /// </code>
    /// </example>
    internal sealed class JObjectPayloadWriter : IPayloadWriter
    {
        private readonly JObject target;

        internal JObjectPayloadWriter(JObject target)
        {
            this.target = target;
        }

        /// <summary>Campo de texto.</summary>
        /// <example><code>writer.WriteText("username", "one");</code></example>
        public void WriteText(string field, string value) => Add(field, new JValue(value));

        /// <summary>Campo inteiro.</summary>
        /// <example><code>writer.WriteInteger("deck_id", 4);</code></example>
        public void WriteInteger(string field, long value) => Add(field, new JValue(value));

        /// <summary>Campo booleano.</summary>
        /// <example><code>writer.WriteBoolean("keep_hand", true);</code></example>
        public void WriteBoolean(string field, bool value) => Add(field, new JValue(value));

        /// <summary>Campo objeto.</summary>
        /// <example><code>writer.WriteObject("payload", payload => payload.WriteInteger("deck_id", 4));</code></example>
        public void WriteObject(string field, Action<IPayloadWriter> writeFields)
        {
            JObject child = new JObject();
            writeFields(new JObjectPayloadWriter(child));
            Add(field, child);
        }

        /// <summary>Campo lista de inteiros.</summary>
        /// <example><code>writer.WriteIntegerList("card_ids", new long[] { 1, 2 });</code></example>
        public void WriteIntegerList(string field, IReadOnlyList<long> values)
        {
            Add(field, new JArray(values.Select(value => new JValue(value))));
        }

        private void Add(string field, JToken value)
        {
            if (target.ContainsKey(field))
                throw new ArgumentException($"field '{field}' is already written: expected each field once per object", nameof(field));

            target.Add(field, value);
        }
    }
}
