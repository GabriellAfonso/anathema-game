#nullable enable
using System;
using Anathema.Net.Account;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Rotas de conta do backend local (<c>docker compose up</c>), com os mesmos caminhos do
    /// <c>AppConfig_Dev</c>. Usadas pela composição com fakes e pelos testes LiveServer.
    /// </summary>
    internal static class LocalAccountRoutes
    {
        internal const string HttpBase = "http://127.0.0.1:8000";

        internal static AccountRoutes Create()
        {
            return new AccountRoutes(Url("/accounts/register/"), Url("/accounts/login/"), Url("/accounts/token/refresh/"),
                Url("/players/me/"), Url("/game/cards/"), Url("/players/decks/"), Url("/game/matches/"));
        }

        private static Uri Url(string path) => new Uri(HttpBase + path);
    }
}
