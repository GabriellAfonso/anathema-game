#nullable enable
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Vista só-leitura de um objeto JSON. É o único jeito de o resto do código ler
    /// payload: os tipos da biblioteca de JSON não saem do codec (constituição,
    /// princípio IV). Leitura obrigatória com forma errada lança
    /// <see cref="PayloadShapeException"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// string code = payload.ReadText("code");
    /// long? deckId = payload.ReadOptionalInteger("deck_id");
    /// </code>
    /// </example>
    public interface IPayloadReader
    {
        /// <summary>Caminho deste objeto, para mensagens; vazio na raiz.</summary>
        /// <example><code>string where = reader.Path;</code></example>
        string Path { get; }

        /// <summary>Nomes dos campos presentes.</summary>
        /// <example><code>bool extras = reader.FieldNames.Count &gt; 2;</code></example>
        IReadOnlyCollection<string> FieldNames { get; }

        /// <summary>O campo existe (mesmo com valor <c>null</c>).</summary>
        /// <example><code>if (reader.Has("deck_problems")) ReadProblems(reader);</code></example>
        bool Has(string field);

        /// <summary>Texto obrigatório.</summary>
        /// <example><code>string error = payload.ReadText("error");</code></example>
        string ReadText(string field);

        /// <summary>Inteiro JSON obrigatório; <c>4.0</c> e <c>"4"</c> são tipo errado.</summary>
        /// <example><code>long deckId = payload.ReadInteger("deck_id");</code></example>
        long ReadInteger(string field);

        /// <summary>Booleano obrigatório.</summary>
        /// <example><code>bool taken = you.ReadBoolean("mulligan_taken");</code></example>
        bool ReadBoolean(string field);

        /// <summary>Objeto obrigatório.</summary>
        /// <example><code>IPayloadReader self = payload.ReadObject("self");</code></example>
        IPayloadReader ReadObject(string field);

        /// <summary>Lista obrigatória de objetos.</summary>
        /// <example><code>IReadOnlyList&lt;IPayloadReader&gt; problems = payload.ReadObjectList("deck_problems");</code></example>
        IReadOnlyList<IPayloadReader> ReadObjectList(string field);

        /// <summary>Lista obrigatória de inteiros.</summary>
        /// <example><code>IReadOnlyList&lt;long&gt; cards = payload.ReadIntegerList("card_ids");</code></example>
        IReadOnlyList<long> ReadIntegerList(string field);

        /// <summary>Texto opcional: nulo se ausente ou <c>null</c>; tipo errado lança.</summary>
        /// <example><code>string? reason = payload.ReadOptionalText("reason");</code></example>
        string? ReadOptionalText(string field);

        /// <summary>Inteiro opcional: nulo se ausente ou <c>null</c>; tipo errado lança.</summary>
        /// <example><code>long? deckId = payload.ReadOptionalInteger("deck_id");</code></example>
        long? ReadOptionalInteger(string field);

        /// <summary>Objeto opcional: nulo se ausente ou <c>null</c>; tipo errado lança.</summary>
        /// <example><code>IPayloadReader? outcome = view.ReadOptionalObject("outcome");</code></example>
        IPayloadReader? ReadOptionalObject(string field);
    }
}
