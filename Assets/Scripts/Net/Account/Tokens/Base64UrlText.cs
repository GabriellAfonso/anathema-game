#nullable enable
using System;
using System.Text;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Base64url sem preenchimento (RFC 7515, §2), a codificação das partes de um JWT. As
    /// mensagens de problema nunca ecoam o texto: ele é pedaço de um token.
    /// </summary>
    /// <example>
    /// <code>
    /// if (Base64UrlText.TryDecode(parts[1], out string claimsJson, out string problem)) Read(claimsJson);
    /// </code>
    /// </example>
    internal static class Base64UrlText
    {
        /// <summary>Decodifica para texto UTF-8; devolve falso com o motivo.</summary>
        /// <example><code>bool ok = Base64UrlText.TryDecode("fn5-", out string text, out string problem); // "~~~"</code></example>
        internal static bool TryDecode(string text, out string decoded, out string problem)
        {
            decoded = string.Empty;
            string? standard = ToPaddedBase64(text, out problem);
            if (standard == null)
                return false;

            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(standard));
                return true;
            }
            catch (FormatException)
            {
                problem = $"segment of {text.Length} characters is not base64url: expected only A-Z, a-z, 0-9, '-' and '_'";
                return false;
            }
        }

        private static string? ToPaddedBase64(string text, out string problem)
        {
            problem = string.Empty;
            int remainder = text.Length % 4;
            if (remainder == 1)
            {
                problem = $"segment has {text.Length} characters: expected a base64url length, never 4n+1";
                return null;
            }

            string standard = text.Replace('-', '+').Replace('_', '/');
            return remainder == 0 ? standard : standard + new string('=', 4 - remainder);
        }
    }
}
