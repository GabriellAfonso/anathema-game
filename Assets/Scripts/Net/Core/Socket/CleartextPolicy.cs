#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Decide se uma URL sem TLS pode abrir. Existe no projeto porque nem o
    /// <c>insecureHttpOption</c> do Unity nem a Network Security Config do Android
    /// alcançam o <c>ClientWebSocket</c>: sem esta política, um build de produção
    /// abriria <c>ws://</c> (specs/001-server-connection/research.md, R3).
    /// </summary>
    /// <example>
    /// <code>
    /// CleartextPolicy policy = new CleartextPolicy(allowsCleartext: Debug.isDebugBuild);
    /// if (!policy.Permits(url)) RefuseCleartext();
    /// </code>
    /// </example>
    public sealed class CleartextPolicy
    {
        /// <summary>Cria a política; só a composição decide o valor.</summary>
        /// <example><code>CleartextPolicy production = new CleartextPolicy(false);</code></example>
        public CleartextPolicy(bool allowsCleartext)
        {
            AllowsCleartext = allowsCleartext;
        }

        /// <summary>Verdadeiro só em editor e build de desenvolvimento.</summary>
        /// <example><code>bool development = policy.AllowsCleartext;</code></example>
        public bool AllowsCleartext { get; }

        /// <summary><c>https</c>/<c>wss</c> sempre; <c>http</c>/<c>ws</c> só se permitido; outro esquema nunca.</summary>
        /// <example><code>bool ok = policy.Permits(new Uri("wss://api.anathema.com/ws/matchmaking/"));</code></example>
        public bool Permits(Uri url)
        {
            if (url == null || !url.IsAbsoluteUri)
                throw new ArgumentException($"url is '{url}': expected an absolute http, https, ws or wss url", nameof(url));

            string scheme = url.Scheme.ToLowerInvariant();
            if (scheme == "https" || scheme == "wss")
                return true;

            return AllowsCleartext && (scheme == "http" || scheme == "ws");
        }
    }
}
