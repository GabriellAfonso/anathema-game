#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountRoutesTests
    {
        [Test]
        public void UrlDeUmDeckAcrescentaOIdentificadorEABarra()
        {
            Assert.That(TestRoutes().Deck(new DeckId(4)).ToString(), Is.EqualTo("http://127.0.0.1:8000/players/decks/4/"));
        }

        [Test]
        public void PaginaDoHistoricoLevaPaginaETamanho()
        {
            Uri url = TestRoutes().MatchesPage(new HistoryPageRequest(2, 50));

            Assert.That(url.ToString(), Is.EqualTo("http://127.0.0.1:8000/game/matches/?page=2&page_size=50"));
        }

        [Test]
        public void RotaRelativaLancaComONomeDaRota()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new AccountRoutes(
                Url("/accounts/register/"), new Uri("/accounts/login/", UriKind.Relative), Url("/accounts/token/refresh/"),
                Url("/players/me/"), Url("/game/cards/"), Url("/players/decks/"), Url("/game/matches/")));

            Assert.That(error.Message, Does.Contain("login"));
        }

        [Test]
        public void ColecaoDeDecksSemBarraFinalLanca()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new AccountRoutes(
                Url("/accounts/register/"), Url("/accounts/login/"), Url("/accounts/token/refresh/"),
                Url("/players/me/"), Url("/game/cards/"), Url("/players/decks"), Url("/game/matches/")));

            Assert.That(error.Message, Does.Contain("decks"));
        }

        internal static AccountRoutes TestRoutes()
        {
            return new AccountRoutes(Url("/accounts/register/"), Url("/accounts/login/"), Url("/accounts/token/refresh/"),
                Url("/players/me/"), Url("/game/cards/"), Url("/players/decks/"), Url("/game/matches/"));
        }

        private static Uri Url(string path) => new Uri("http://127.0.0.1:8000" + path);
    }
}
