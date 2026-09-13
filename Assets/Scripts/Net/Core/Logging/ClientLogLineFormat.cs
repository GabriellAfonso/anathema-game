#nullable enable
using System.Text;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Forma de uma linha de log: <c>event_name key=value key="valor com espaço"</c>.
    /// Uma linha por registro para que <c>adb logcat -s Unity</c> continue legível
    /// e filtrável por nome de evento.
    /// </summary>
    /// <example>
    /// <code>
    /// string line = ClientLogLineFormat.Format(entry); // socket_closed close_code=4001
    /// </code>
    /// </example>
    public static class ClientLogLineFormat
    {
        /// <summary>Formata a entrada numa linha.</summary>
        /// <example><code>UnityEngine.Debug.Log(ClientLogLineFormat.Format(entry));</code></example>
        public static string Format(ClientLogEntry entry)
        {
            StringBuilder line = new StringBuilder(entry.EventName);
            foreach (LogField field in entry.Fields)
                line.Append(' ').Append(field.Name).Append('=').Append(QuoteIfNeeded(field.Value));

            return line.ToString();
        }

        private static string QuoteIfNeeded(string value)
        {
            if (!NeedsQuotes(value))
                return value;

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static bool NeedsQuotes(string value)
        {
            if (value.Length == 0)
                return true;

            foreach (char character in value)
            {
                if (char.IsWhiteSpace(character) || character == '"' || character == '=')
                    return true;
            }

            return false;
        }
    }
}
