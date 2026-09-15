#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>contracts/composition-and-scenes.md: as nove rotas de quem compõe.</summary>
    public class ServerRoutesTests
    {
        [Test]
        public void PorHostMontaAsRotasDaContaEDosSockets()
        {
            ServerRoutes routes = ServerRoutes.ForHost("http://192.168.0.10:8000/", "ws://192.168.0.10:8000");

            Assert.That(routes.Account.Login.ToString(), Is.EqualTo("http://192.168.0.10:8000/accounts/login/"));
            Assert.That(routes.Account.Matches.ToString(), Is.EqualTo("http://192.168.0.10:8000/game/matches/"));
            Assert.That(routes.Connection.Matchmaking.ToString(), Is.EqualTo("ws://192.168.0.10:8000/ws/matchmaking/"));
            Assert.That(routes.Connection.Match.ToString(), Is.EqualTo("ws://192.168.0.10:8000/ws/match/"));
        }

        [TestCase("")]
        [TestCase("192.168.0.10:8000")]
        public void BaseInvalidaLancaCitandoOValor(string httpBase)
        {
            ArgumentException thrown = Assert.Throws<ArgumentException>(() => ServerRoutes.ForHost(httpBase, "ws://127.0.0.1:8000"));

            Assert.That(thrown.Message, Does.Contain("httpBase"));
        }

        [Test]
        public void RotaNulaLanca()
        {
            Uri any = new Uri("http://127.0.0.1:8000/x/");
            Uri socket = new Uri("ws://127.0.0.1:8000/ws/matchmaking/");

            // AccountRoutes e ConnectionRoutes recusam a URL nula junto com as malformadas, como ArgumentException.
            Assert.Catch<ArgumentException>(() => new ServerRoutes(any, any, any, any, any, any, any, socket, null!));
            Assert.Catch<ArgumentException>(() => new ServerRoutes(null!, any, any, any, any, any, any, socket, socket));
        }
    }
}
