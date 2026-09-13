#nullable enable
using System;
using System.Globalization;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um campo nomeado de um registro de log. Campos em vez de texto montado
    /// para que o log de depuração possa ser filtrado por nome.
    /// </summary>
    /// <example>
    /// <code>
    /// log.Info("socket_closed", new LogField("close_code", 4001));
    /// </code>
    /// </example>
    public readonly struct LogField
    {
        /// <summary>Campo de texto.</summary>
        /// <example><code>LogField host = new LogField("host", "192.168.0.10:8000");</code></example>
        public LogField(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"log field name is '{name}': expected a non-empty snake_case name", nameof(name));

            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(value), $"log field '{name}' value is null: expected text");
        }

        /// <summary>Campo inteiro, formatado sem separador de milhar.</summary>
        /// <example><code>LogField code = new LogField("close_code", 4001);</code></example>
        public LogField(string name, long value)
            : this(name, value.ToString(CultureInfo.InvariantCulture))
        {
        }

        /// <summary>Campo booleano, como <c>true</c>/<c>false</c>.</summary>
        /// <example><code>LogField matched = new LogField("marker_matched", true);</code></example>
        public LogField(string name, bool value)
            : this(name, value ? "true" : "false")
        {
        }

        /// <summary>Nome do campo, em snake_case.</summary>
        /// <example><code>string name = field.Name;</code></example>
        public string Name { get; }

        /// <summary>Valor já em texto.</summary>
        /// <example><code>string value = field.Value;</code></example>
        public string Value { get; }
    }
}
