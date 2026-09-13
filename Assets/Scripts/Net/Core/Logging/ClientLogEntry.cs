#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um registro de depuração: nível, nome de evento estável e campos nomeados.
    /// O nome de evento é estável para que busca no logcat não quebre quando a
    /// frase mudar.
    /// </summary>
    /// <example>
    /// <code>
    /// ClientLogEntry entry = new ClientLogEntry(ClientLogLevel.Info, "socket_opened", new[] { new LogField("url", url) });
    /// </code>
    /// </example>
    public sealed class ClientLogEntry
    {
        /// <summary>Cria o registro; os campos são copiados.</summary>
        /// <example><code>ClientLogEntry entry = new ClientLogEntry(ClientLogLevel.Error, "codec_unexpected_failure", fields);</code></example>
        public ClientLogEntry(ClientLogLevel level, string eventName, IReadOnlyList<LogField> fields)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException($"log event name is '{eventName}': expected a non-empty snake_case name", nameof(eventName));

            Level = level;
            EventName = eventName;
            Fields = new List<LogField>(fields ?? Array.Empty<LogField>()).AsReadOnly();
        }

        /// <summary>Gravidade.</summary>
        /// <example><code>bool isError = entry.Level == ClientLogLevel.Error;</code></example>
        public ClientLogLevel Level { get; }

        /// <summary>Nome estável do evento, em snake_case.</summary>
        /// <example><code>bool closed = entry.EventName == "socket_closed";</code></example>
        public string EventName { get; }

        /// <summary>Campos na ordem em que foram passados.</summary>
        /// <example><code>LogField first = entry.Fields[0];</code></example>
        public IReadOnlyList<LogField> Fields { get; }
    }
}
