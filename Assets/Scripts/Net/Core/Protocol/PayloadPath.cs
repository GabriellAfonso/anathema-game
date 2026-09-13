#nullable enable
using System.Globalization;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Monta o caminho de um campo para as mensagens de falha, no formato
    /// <c>payload.deck_problems[0].kind</c>. Um lugar só para o reader, a união e as
    /// extensões de identidade escreverem o mesmo caminho.
    /// </summary>
    /// <example>
    /// <code>
    /// string path = PayloadPath.Field(reader.Path, "user_id");
    /// </code>
    /// </example>
    public static class PayloadPath
    {
        /// <summary>Caminho de um campo dentro de <paramref name="parent"/>; raiz é vazio.</summary>
        /// <example><code>string path = PayloadPath.Field("payload", "code"); // payload.code</code></example>
        public static string Field(string parent, string field)
        {
            return parent.Length == 0 ? field : parent + "." + field;
        }

        /// <summary>Caminho de um item de lista.</summary>
        /// <example><code>string path = PayloadPath.Item("payload.deck_problems", 1); // payload.deck_problems[1]</code></example>
        public static string Item(string list, int index)
        {
            return list + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }
    }
}
