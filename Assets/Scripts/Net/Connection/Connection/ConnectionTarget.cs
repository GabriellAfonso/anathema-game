#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Para onde a conexão abre: o socket de fila ou o de uma partida. O token entra só na hora de
    /// abrir, na query string, porque o servidor o lê dali e nunca de cabeçalho; e o texto deste alvo,
    /// que vai para o log, nunca mostra a query (FR-002, FR-016).
    /// </summary>
    /// <example>
    /// <code>
    /// ConnectionTarget target = ConnectionTarget.Match(routes.Match, pairing.Match);
    /// socket.Open(target.WithToken(token));
    /// </code>
    /// </example>
    internal sealed class ConnectionTarget : IEquatable<ConnectionTarget>
    {
        private const string MatchParameter = "matchId";
        private const string TokenParameter = "token";

        private readonly string? matchId;

        private ConnectionTarget(Uri baseUrl, string? matchId)
        {
            BaseUrl = baseUrl;
            this.matchId = matchId;
        }

        /// <summary>URL do socket, sem query.</summary>
        /// <example><code>Uri url = target.BaseUrl;</code></example>
        public Uri BaseUrl { get; }

        /// <summary>O socket de fila.</summary>
        /// <example><code>ConnectionTarget target = ConnectionTarget.Matchmaking(routes.Matchmaking);</code></example>
        public static ConnectionTarget Matchmaking(Uri baseUrl)
        {
            return new ConnectionTarget(RequireWithoutQuery(baseUrl), null);
        }

        /// <summary>O socket de uma partida.</summary>
        /// <example><code>ConnectionTarget target = ConnectionTarget.Match(routes.Match, new MatchId("5b7c…"));</code></example>
        public static ConnectionTarget Match(Uri baseUrl, MatchId match)
        {
            if (match.Value.Length == 0)
                throw new ArgumentException($"match is '{match}': expected a match_id read from match_found", nameof(match));

            return new ConnectionTarget(RequireWithoutQuery(baseUrl), match.Value);
        }

        /// <summary>A URL que abre o socket, com o token escapado na query.</summary>
        /// <example><code>Uri url = target.WithToken(token);</code></example>
        public Uri WithToken(AccessToken token)
        {
            AccessToken required = token ?? throw new ArgumentNullException(nameof(token), $"access token is null for {this}: expected the token from IAccessTokenSource.GetValidAsync");
            string match = matchId == null ? string.Empty : $"{MatchParameter}={Uri.EscapeDataString(matchId)}&";
            return new Uri($"{BaseUrl.GetLeftPart(UriPartial.Path)}?{match}{TokenParameter}={Uri.EscapeDataString(required.RevealForRequest())}");
        }

        /// <inheritdoc />
        public bool Equals(ConnectionTarget? other)
        {
            return other != null && BaseUrl.Equals(other.BaseUrl) && string.Equals(matchId, other.matchId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as ConnectionTarget);

        /// <inheritdoc />
        public override int GetHashCode() => BaseUrl.GetHashCode() ^ (matchId == null ? 0 : StringComparer.Ordinal.GetHashCode(matchId));

        /// <summary>Esquema, host e caminho; nunca a query, que carrega o token.</summary>
        /// <example><code>log.Info("connection_opening", new LogField("target", target.ToString()));</code></example>
        public override string ToString() => BaseUrl.GetLeftPart(UriPartial.Path);

        private static Uri RequireWithoutQuery(Uri baseUrl)
        {
            Uri url = SocketUrl.RequireAbsoluteSocket(baseUrl, nameof(baseUrl));
            if (url.Query.Length > 0)
                throw new ArgumentException($"baseUrl is '{url}': expected a socket url without query, the connection adds matchId and token", nameof(baseUrl));

            return url;
        }
    }
}
