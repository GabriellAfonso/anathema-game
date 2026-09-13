#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um pedido HTTP. Todo pedido tem prazo (FR-013): sem ele, um servidor mudo
    /// prende a tela de login para sempre.
    /// </summary>
    /// <example>
    /// <code>
    /// HttpRequestSpec request = new HttpRequestSpec("GET", new Uri(httpBase + "/game/cards/"),
    ///     new Dictionary&lt;string, string&gt; { ["Authorization"] = "Bearer " + token });
    /// </code>
    /// </example>
    public sealed class HttpRequestSpec
    {
        /// <summary>Prazo padrão em segundos, o mesmo do smoke_match.py do backend.</summary>
        /// <example><code>int seconds = HttpRequestSpec.DefaultTimeoutSeconds;</code></example>
        public const int DefaultTimeoutSeconds = 10;

        private static readonly HashSet<string> AllowedMethods = new HashSet<string> { "GET", "POST", "PUT", "PATCH", "DELETE" };

        /// <summary>Cria o pedido validando método, URL e prazo.</summary>
        /// <example><code>HttpRequestSpec login = new HttpRequestSpec("POST", loginUrl, JsonHeaders, body);</code></example>
        public HttpRequestSpec(string method, Uri url, IReadOnlyDictionary<string, string>? headers = null,
            string? body = null, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            Method = RequireMethod(method);
            Url = RequireAbsolute(url);
            Headers = new Dictionary<string, string>(ToDictionary(headers));
            Body = body;
            TimeoutSeconds = RequireTimeout(timeoutSeconds);
        }

        /// <summary>GET, POST, PUT, PATCH ou DELETE.</summary>
        /// <example><code>string method = request.Method;</code></example>
        public string Method { get; }

        /// <summary>URL absoluta.</summary>
        /// <example><code>Uri url = request.Url;</code></example>
        public Uri Url { get; }

        /// <summary>Cabeçalhos, copiados na construção.</summary>
        /// <example><code>bool authenticated = request.Headers.ContainsKey("Authorization");</code></example>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Corpo em texto, ou nulo.</summary>
        /// <example><code>string? body = request.Body;</code></example>
        public string? Body { get; }

        /// <summary>Prazo em segundos, de 1 a 120.</summary>
        /// <example><code>int seconds = request.TimeoutSeconds;</code></example>
        public int TimeoutSeconds { get; }

        private static string RequireMethod(string method)
        {
            if (method != null && AllowedMethods.Contains(method))
                return method;

            throw new ArgumentException($"http method is '{method}': expected GET, POST, PUT, PATCH or DELETE", nameof(method));
        }

        private static Uri RequireAbsolute(Uri url)
        {
            if (url != null && url.IsAbsoluteUri)
                return url;

            throw new ArgumentException($"http url is '{url}': expected an absolute url like http://host:8000/path", nameof(url));
        }

        private static int RequireTimeout(int timeoutSeconds)
        {
            if (timeoutSeconds >= 1 && timeoutSeconds <= 120)
                return timeoutSeconds;

            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, $"http timeout is {timeoutSeconds}s: expected 1 to 120 seconds");
        }

        private static IDictionary<string, string> ToDictionary(IReadOnlyDictionary<string, string>? headers)
        {
            Dictionary<string, string> copy = new Dictionary<string, string>();
            if (headers == null)
                return copy;

            foreach (KeyValuePair<string, string> header in headers)
                copy[header.Key] = header.Value;

            return copy;
        }
    }
}
