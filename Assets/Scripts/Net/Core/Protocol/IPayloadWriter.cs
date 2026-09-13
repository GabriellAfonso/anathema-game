#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Escreve os campos de um objeto JSON sem expor a biblioteca de JSON. Campo
    /// repetido lança <see cref="ArgumentException"/> com o nome.
    /// </summary>
    /// <example>
    /// <code>
    /// public void WritePayload(IPayloadWriter writer) => writer.WriteInteger("deck_id", deckId);
    /// </code>
    /// </example>
    public interface IPayloadWriter
    {
        /// <summary>Campo de texto.</summary>
        /// <example><code>writer.WriteText("username", username);</code></example>
        void WriteText(string field, string value);

        /// <summary>Campo inteiro.</summary>
        /// <example><code>writer.WriteInteger("deck_id", 4);</code></example>
        void WriteInteger(string field, long value);

        /// <summary>Campo booleano.</summary>
        /// <example><code>writer.WriteBoolean("keep_hand", true);</code></example>
        void WriteBoolean(string field, bool value);

        /// <summary>Campo objeto, com os campos escritos pelo delegate.</summary>
        /// <example><code>writer.WriteObject("target", target => target.WriteCardInstanceId("card_instance_id", id));</code></example>
        void WriteObject(string field, Action<IPayloadWriter> writeFields);

        /// <summary>Campo lista de inteiros.</summary>
        /// <example><code>writer.WriteIntegerList("card_ids", new long[] { 1, 2 });</code></example>
        void WriteIntegerList(string field, IReadOnlyList<long> values);
    }
}
