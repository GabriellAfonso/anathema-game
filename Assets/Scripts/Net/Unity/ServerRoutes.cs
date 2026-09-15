#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// As rotas HTTP da conta e dos dois sockets, juntas, para quem compõe. O hospedeiro monta pelo
    /// <c>AppConfig</c>; a prova monta pelo host do servidor (specs/005-presentation-facade/contracts/composition-and-scenes.md).
    /// </summary>
    /// <example>
    /// <code>
    /// ServerRoutes routes = ServerRoutes.ForHost("http://127.0.0.1:8000", "ws://127.0.0.1:8000");
    /// ComposedClient composed = ClientComposition.Compose(new ClientCompositionOptions(routes, allowCleartext: true));
    /// </code>
    /// </example>
    public sealed class ServerRoutes
    {
        /// <summary>As nove rotas; nenhuma pode ser nula.</summary>
        /// <example><code>ServerRoutes routes = new ServerRoutes(register, login, refresh, profile, cards, decks, matches, matchmaking, match);</code></example>
        public ServerRoutes(Uri register, Uri login, Uri refresh, Uri ownProfile, Uri cards, Uri decks, Uri matches, Uri matchmaking, Uri match)
        {
            Account = new AccountRoutes(register, login, refresh, ownProfile, cards, decks, matches);
            Connection = new ConnectionRoutes(matchmaking, match);
        }

        internal ServerRoutes(AccountRoutes account, ConnectionRoutes connection)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account), "account routes are null: expected AppConfig.BuildAccountRoutes()");
            Connection = connection ?? throw new ArgumentNullException(nameof(connection), "connection routes are null: expected AppConfig.BuildConnectionRoutes()");
        }

        internal AccountRoutes Account { get; }

        internal ConnectionRoutes Connection { get; }

        /// <summary>
        /// As rotas do backend num host, com os caminhos de <c>backend/specs/011-deck-catalog-api/contracts/</c> e do
        /// cadastro, login e perfil da 002; bases sem barra final.
        /// </summary>
        /// <example><code>ServerRoutes routes = ServerRoutes.ForHost("http://192.168.0.10:8000", "ws://192.168.0.10:8000");</code></example>
        public static ServerRoutes ForHost(string httpBase, string wsBase)
        {
            string http = RequireBase(httpBase, nameof(httpBase)), ws = RequireBase(wsBase, nameof(wsBase));
            return new ServerRoutes(new Uri(http + "/accounts/register/"), new Uri(http + "/accounts/login/"), new Uri(http + "/accounts/token/refresh/"),
                new Uri(http + "/players/me/"), new Uri(http + "/game/cards/"), new Uri(http + "/players/decks/"), new Uri(http + "/game/matches/"),
                new Uri(ws + "/ws/matchmaking/"), new Uri(ws + "/ws/match/"));
        }

        /// <summary>Forma para log, sem credencial.</summary>
        /// <example><code>string text = routes.ToString(); // login=http://127.0.0.1:8000/accounts/login/ match=ws://...</code></example>
        public override string ToString() => $"login={Account.Login} match={Connection.Match}";

        private static string RequireBase(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || !Uri.IsWellFormedUriString(value, UriKind.Absolute))
                throw new ArgumentException($"{name} is '{value}': expected an absolute base such as http://127.0.0.1:8000", name);

            return value.TrimEnd('/');
        }
    }
}
