#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionTargetTests
    {
        private FakeAccessTokenSource tokens = null!;

        [SetUp]
        public void CreateTokens()
        {
            tokens = new FakeAccessTokenSource(new FakeMonotonicClock());
        }

        [Test]
        public async Task FilaLevaSoOToken()
        {
            Uri url = ConnectionTestRig.MatchmakingTarget.WithToken(await TokenAsync("abc"));

            Assert.That(url.AbsoluteUri, Is.EqualTo(ConnectionTestRig.MatchmakingBase + "?token=abc"));
        }

        [Test]
        public async Task PartidaLevaMatchIdEToken()
        {
            Uri url = ConnectionTestRig.MatchTarget("m-1").WithToken(await TokenAsync("abc"));

            Assert.That(url.Query, Is.EqualTo("?matchId=m-1&token=abc"));
        }

        [Test]
        public async Task TokenEhEscapadoNaQuery()
        {
            Uri url = ConnectionTestRig.MatchmakingTarget.WithToken(await TokenAsync("a+b/c="));

            Assert.That(url.Query, Is.EqualTo("?token=a%2Bb%2Fc%3D"));
        }

        [TestCase("http://127.0.0.1:8000/ws/matchmaking/")]
        [TestCase("ws://127.0.0.1:8000/ws/matchmaking/?token=x")]
        public void UrlQueNaoEhBaseDeSocketLancaComOValor(string raw)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => ConnectionTarget.Matchmaking(new Uri(raw)));

            Assert.That(error.Message, Does.Contain(raw).And.Contain("expected"));
        }

        [Test]
        public void UrlRelativaLanca()
        {
            Assert.Throws<ArgumentException>(() => ConnectionTarget.Matchmaking(new Uri("/ws/matchmaking/", UriKind.Relative)));
        }

        [Test]
        public void TextoDoAlvoNuncaMostraQuery()
        {
            Assert.That(ConnectionTestRig.MatchTarget("m-1").ToString(), Is.EqualTo(ConnectionTestRig.MatchBase));
        }

        [Test]
        public void IgualdadePorUrlEPartida()
        {
            Assert.That(ConnectionTestRig.MatchTarget("m-1"), Is.EqualTo(ConnectionTestRig.MatchTarget("m-1")));
            Assert.That(ConnectionTestRig.MatchTarget("m-1"), Is.Not.EqualTo(ConnectionTestRig.MatchTarget("m-2")));
            Assert.That(ConnectionTestRig.MatchmakingTarget, Is.Not.EqualTo(ConnectionTestRig.MatchTarget("m-1")));
        }

        [Test]
        public void RotasValidamEsquemaDeSocket()
        {
            ConnectionRoutes routes = new ConnectionRoutes(new Uri(ConnectionTestRig.MatchmakingBase), new Uri(ConnectionTestRig.MatchBase));

            Assert.That(routes.Match.AbsoluteUri, Is.EqualTo(ConnectionTestRig.MatchBase));
            Assert.Throws<ArgumentException>(() => new ConnectionRoutes(new Uri("http://127.0.0.1:8000/ws/matchmaking/"), new Uri(ConnectionTestRig.MatchBase)));
            Assert.Throws<ArgumentException>(() => new ConnectionRoutes(new Uri(ConnectionTestRig.MatchmakingBase), null!));
        }

        private async Task<AccessToken> TokenAsync(string text)
        {
            tokens.EnqueueValid(text);
            AccessTokenOutcome outcome = await tokens.GetValidAsync();
            return outcome.Token!;
        }
    }
}
