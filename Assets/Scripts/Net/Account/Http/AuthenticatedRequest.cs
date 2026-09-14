#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Um pedido a uma rota autenticada, antes de receber o token. O cliente autenticado põe os
    /// cabeçalhos; quem monta o pedido só diz método, URL e corpo JSON.
    /// </summary>
    /// <example>
    /// <code>
    /// AuthenticatedRequest create = new AuthenticatedRequest("POST", routes.Decks, body);
    /// AuthenticatedRequest read = AuthenticatedRequest.Get(routes.Cards);
    /// </code>
    /// </example>
    public sealed class AuthenticatedRequest
    {
        private static readonly HashSet<string> AllowedMethods = new HashSet<string> { "GET", "POST", "PATCH", "DELETE" };

        /// <summary>Cria o pedido; método fora de GET, POST, PATCH e DELETE, ou URL relativa, lança.</summary>
        /// <example><code>AuthenticatedRequest remove = new AuthenticatedRequest("DELETE", routes.Deck(deck));</code></example>
        public AuthenticatedRequest(string method, Uri url, string? jsonBody = null)
        {
            if (method == null || !AllowedMethods.Contains(method))
                throw new ArgumentException($"authenticated request method is '{method}': expected GET, POST, PATCH or DELETE", nameof(method));

            if (url == null || !url.IsAbsoluteUri)
                throw new ArgumentException($"authenticated request url is '{url}': expected an absolute url", nameof(url));

            Method = method;
            Url = url;
            JsonBody = jsonBody;
        }

        /// <summary>Método HTTP.</summary>
        /// <example><code>string method = request.Method;</code></example>
        public string Method { get; }

        /// <summary>URL absoluta.</summary>
        /// <example><code>Uri url = request.Url;</code></example>
        public Uri Url { get; }

        /// <summary>Corpo JSON, ou nulo.</summary>
        /// <example><code>bool hasBody = request.JsonBody != null;</code></example>
        public string? JsonBody { get; }

        /// <summary>GET sem corpo.</summary>
        /// <example><code>AuthenticatedRequest read = AuthenticatedRequest.Get(routes.OwnProfile);</code></example>
        public static AuthenticatedRequest Get(Uri url) => new AuthenticatedRequest("GET", url);
    }
}
