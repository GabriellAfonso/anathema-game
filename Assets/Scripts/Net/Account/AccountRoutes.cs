#nullable enable
using System;
using System.Globalization;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// URLs absolutas das rotas HTTP de conta e dados do jogador, montadas a partir do
    /// <c>AppConfig</c>. As rotas de coleção terminam em barra, como o Django exige.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountRoutes routes = AppEnvManager.Settings.BuildAccountRoutes();
    /// Uri deck = routes.Deck(new DeckId(4)); // http://host/players/decks/4/
    /// </code>
    /// </example>
    public sealed class AccountRoutes
    {
        /// <summary>Cria as rotas; URL relativa, ou decks sem barra final, lança.</summary>
        /// <example><code>AccountRoutes routes = new AccountRoutes(register, login, refresh, me, cards, decks, matches);</code></example>
        public AccountRoutes(Uri register, Uri login, Uri refresh, Uri ownProfile, Uri cards, Uri decks, Uri matches)
        {
            Register = RequireAbsolute(register, nameof(register));
            Login = RequireAbsolute(login, nameof(login));
            Refresh = RequireAbsolute(refresh, nameof(refresh));
            OwnProfile = RequireAbsolute(ownProfile, nameof(ownProfile));
            Cards = RequireAbsolute(cards, nameof(cards));
            Decks = RequireCollection(decks, nameof(decks));
            Matches = RequireAbsolute(matches, nameof(matches));
        }

        /// <summary><c>POST</c> de cadastro.</summary>
        /// <example><code>Uri url = routes.Register;</code></example>
        public Uri Register { get; }

        /// <summary><c>POST</c> de login.</summary>
        /// <example><code>Uri url = routes.Login;</code></example>
        public Uri Login { get; }

        /// <summary><c>POST</c> de renovação do token de acesso.</summary>
        /// <example><code>Uri url = routes.Refresh;</code></example>
        public Uri Refresh { get; }

        /// <summary><c>GET</c> do próprio perfil.</summary>
        /// <example><code>Uri url = routes.OwnProfile;</code></example>
        public Uri OwnProfile { get; }

        /// <summary><c>GET</c> do catálogo.</summary>
        /// <example><code>Uri url = routes.Cards;</code></example>
        public Uri Cards { get; }

        /// <summary>Coleção de decks do autenticado.</summary>
        /// <example><code>Uri url = routes.Decks;</code></example>
        public Uri Decks { get; }

        /// <summary><c>GET</c> do histórico, sem consulta.</summary>
        /// <example><code>Uri url = routes.Matches;</code></example>
        public Uri Matches { get; }

        /// <summary>URL de um deck.</summary>
        /// <example><code>Uri url = routes.Deck(new DeckId(4));</code></example>
        public Uri Deck(DeckId deck)
        {
            return new Uri(Decks, deck.Value.ToString(CultureInfo.InvariantCulture) + "/");
        }

        /// <summary>URL de uma página do histórico.</summary>
        /// <example><code>Uri url = routes.MatchesPage(new HistoryPageRequest(2));</code></example>
        public Uri MatchesPage(HistoryPageRequest request)
        {
            return new Uri(Matches.GetLeftPart(UriPartial.Path) + request.ToQuery());
        }

        private static Uri RequireAbsolute(Uri url, string route)
        {
            if (url != null && url.IsAbsoluteUri)
                return url;

            throw new ArgumentException($"{route} route is '{url}': expected an absolute url like http://host:8000/path/", route);
        }

        private static Uri RequireCollection(Uri url, string route)
        {
            Uri absolute = RequireAbsolute(url, route);
            if (absolute.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
                return absolute;

            throw new ArgumentException($"{route} route is '{url}': expected a collection url ending in '/'", route);
        }
    }
}
