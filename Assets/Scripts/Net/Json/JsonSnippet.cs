#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anathema.Net.Json
{
    /// <summary>
    /// Trecho curto do que chegou, para mensagens de falha. Cortado em 200 caracteres
    /// para um frame de estado inteiro não inundar o log.
    /// </summary>
    internal static class JsonSnippet
    {
        private const int MaxLength = 200;

        internal static string Of(string text)
        {
            return text.Length <= MaxLength ? text : text.Substring(0, MaxLength) + "…";
        }

        internal static string Of(JToken token)
        {
            return token.Type + " " + Of(token.ToString(Formatting.None));
        }
    }
}
