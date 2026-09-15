#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    public class AccountServicesTests
    {
        // O NUnit reaproveita a instância do fixture entre os testes: os fakes nascem no SetUp para
        // um teste não herdar os pedidos roteirizados e registrados pelo anterior.
        private FakeHttpTransport http = null!;
        private FakeMonotonicClock clock = null!;
        private FakeClientLog log = null!;
        private FakeAppLifecycle lifecycle = null!;

        [SetUp]
        public void CreateFakes()
        {
            http = new FakeHttpTransport();
            clock = new FakeMonotonicClock();
            log = new FakeClientLog();
            lifecycle = new FakeAppLifecycle(clock);
        }

        [Test]
        public async Task SessaoTokensEPerfilCompartilhamAMesmaSessao()
        {
            using AccountServices account = Compose(new FakeRefreshTokenVault());
            http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));

            await account.Session.SignInAsync("one", new Password("123456"));
            AccessTokenOutcome token = await account.Tokens.GetValidAsync();

            Assert.That(token.Kind, Is.EqualTo(AccessTokenOutcomeKind.Valid));
            Assert.That(token.Token!.RevealForRequest(), Is.EqualTo(account.Session.CurrentAccessTokenText));
            Assert.That(account.Client, Is.Not.Null);
            Assert.That(account.Profile, Is.Not.Null);
            Assert.That(account.Registration, Is.Not.Null);
            Assert.That(account.Catalog, Is.Not.Null);
            Assert.That(account.Decks, Is.Not.Null);
            Assert.That(account.History, Is.Not.Null);
        }

        [Test]
        public async Task VoltarDepoisDeSeteMinutosRenova()
        {
            using AccountServices account = Compose(new FakeRefreshTokenVault());
            http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));
            await account.Session.SignInAsync("one", new Password("123456"));
            lifecycle.SimulateBackground();
            clock.Advance(TimeSpan.FromMinutes(7));
            http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed")));

            lifecycle.SimulateForeground();

            Assert.That(http.Requests.Count, Is.EqualTo(2));
            Assert.That(account.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
        }

        [Test]
        public async Task SegundaComposicaoComAMesmaGuardaRetomaSemSenha()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();
            using AccountServices first = Compose(vault);
            http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));
            await first.Session.SignInAsync("one", new Password("123456"));
            using AccountServices second = Compose(vault);
            http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("resumed")));

            ResumeOutcome resumed = await second.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed));
            Assert.That(resumed.User, Is.EqualTo(first.Session.Self));
        }

        private AccountServices Compose(IRefreshTokenVault vault)
        {
            IProtocolCodec codec = new NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log);
            ClientPorts ports = new ClientPorts(http, new FakeWebSocketFactory(), clock, new FakeFrameTicker(), lifecycle, new FakeNetworkReachability(NetworkKind.LocalArea),
                new MainThreadQueue(log), log, codec, vault, FacadeTestRig.AccountRoutesForTests(), FacadeTestRig.ConnectionRoutesForTests(), new AccountTiming());
            return new AccountServices(ports);
        }
    }
}
