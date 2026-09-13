#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Host e porta do servidor, sem esquema nem caminho. Aceita o que alguém cola da barra do
    /// navegador (<c>http://192.168.0.10:8000/</c>) e recusa o que não é endereço, para um IP
    /// digitado errado não virar URL quebrada em tempo de execução.
    /// </summary>
    /// <example>
    /// <code>
    /// if (!ServerHost.TryParse(raw, out ServerHost? host, out string problem)) log.Warning("server_host_rejected", new LogField("problem", problem));
    /// </code>
    /// </example>
    public sealed class ServerHost
    {
        private static readonly string[] Schemes = { "https://", "http://", "wss://", "ws://" };

        private ServerHost(string hostAndPort)
        {
            HostAndPort = hostAndPort;
        }

        /// <summary>Host com porta opcional, como <c>192.168.0.10:8000</c>.</summary>
        /// <example><code>string url = $"ws://{host.HostAndPort}/ws/matchmaking/";</code></example>
        public string HostAndPort { get; }

        /// <summary>Normaliza e valida; em recusa, <paramref name="problem"/> traz o valor e a forma esperada.</summary>
        /// <example><code>bool ok = ServerHost.TryParse("http://192.168.0.10:8000/", out ServerHost? host, out string problem);</code></example>
        public static bool TryParse(string? raw, [NotNullWhen(true)] out ServerHost? host, out string problem)
        {
            host = null;
            string candidate = StripScheme((raw ?? string.Empty).Trim()).TrimEnd('/');
            problem = Problem(raw, candidate);
            if (problem.Length > 0)
                return false;

            host = new ServerHost(candidate);
            return true;
        }

        /// <summary>O host e a porta.</summary>
        /// <example><code>string text = host.ToString();</code></example>
        public override string ToString() => HostAndPort;

        private static string StripScheme(string value)
        {
            string? scheme = Schemes.FirstOrDefault(prefix => value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
            return scheme == null ? value : value.Substring(scheme.Length);
        }

        private static string Problem(string? raw, string candidate)
        {
            string expected = $"server host is '{raw}': expected host[:port] like 192.168.0.10:8000";
            if (candidate.Length == 0)
                return expected + ", got an empty value";

            if (candidate.Any(char.IsWhiteSpace) || candidate.Contains("/"))
                return expected + ", without spaces or path";

            return PortProblem(candidate, expected);
        }

        private static string PortProblem(string candidate, string expected)
        {
            int colon = candidate.LastIndexOf(':');
            if (colon < 0)
                return string.Empty;

            bool validPort = int.TryParse(candidate.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int port) && port >= 1 && port <= 65535;
            if (colon == 0 || !validPort)
                return expected + ", with a port from 1 to 65535";

            return string.Empty;
        }
    }
}
