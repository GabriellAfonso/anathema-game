#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US5-1, US5-2, US5-3, FR-010, FR-014: catálogo, recusa e desistência viram partida indisponível, com tentar de novo e voltar.</summary>
    public class MatchUnavailableTests
    {
        private const string MatchDeniedFrame = "{\"type\": \"match_denied\", \"payload\": {\"error\": \"no live match\"}}";
        private const string AuthDeniedFrame = "{\"type\": \"auth_denied\", \"payload\": {\"error\": \"authentication required: invalid token\"}}";

        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task CatalogoFalhandoViraIndisponivelSemAbrirSocketDePartida()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            int created = rig.Sockets.Created.Count;

            rig.ReceiveMatchFound(catalogStatus: 500);

            MatchUnavailable unavailable = rig.Client.State.Unavailable!;
            Assert.That((rig.Client.State.Stage, unavailable.Kind, unavailable.Match.Value), Is.EqualTo((ClientStage.MatchUnavailable, MatchUnavailableKind.CatalogUnavailable, FacadeTestRig.MatchIdText)));
            Assert.That((rig.Sockets.Created.Count, rig.Count("match_catalog_unavailable")), Is.EqualTo((created, 1)));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
            Assert.That(unavailable.PlayerText, Is.Not.Empty);
        }

        [Test]
        public async Task TentarDeNovoComCatalogoCarregandoAbreAPartida()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            rig.ReceiveMatchFound(catalogStatus: 500);
            rig.Http.RespondNext(200, FacadeCatalog.Body());

            StageRequestResult result = rig.Client.RetryMatch();
            rig.Pump();
            rig.OpenLatest();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchStart);

            Assert.That((result.Applied, result.Stage), Is.EqualTo((true, ClientStage.Paired)));
            Assert.That(rig.Sockets.Latest.OpenedUrl!.AbsolutePath, Is.EqualTo("/ws/match/"));
            Assert.That((rig.Client.State.Stage, rig.Client.State.Pairing!.Match.Value), Is.EqualTo((ClientStage.InMatch, FacadeTestRig.MatchIdText)));
        }

        [Test]
        public async Task PartidaRecusadaViraIndisponivelComOMotivo()
        {
            await rig.ReachPairedAsync();
            rig.OpenLatest();

            rig.Receive(MatchDeniedFrame);
            rig.Sockets.Latest.SimulateClosed(4404, "match_denied");
            rig.Advance(TimeSpan.FromSeconds(30));

            MatchUnavailable unavailable = rig.Client.State.Unavailable!;
            Assert.That((rig.Client.State.Stage, unavailable.Kind), Is.EqualTo((ClientStage.MatchUnavailable, MatchUnavailableKind.MatchRefused)));
            Assert.That(unavailable.GiveUp, Is.EqualTo(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound)));
            Assert.That(rig.Client.CurrentMatch, Is.Null);
        }

        [Test]
        public async Task TokenRecusadoRepetidamenteViraIndisponivelPorDesistencia()
        {
            await rig.ReachInMatchAsync();
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed-1")));
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed-2")));

            for (int refusal = 0; refusal < 3; refusal++)
                RefuseLatestToken(firstIsOpen: refusal == 0);

            MatchUnavailable unavailable = rig.Client.State.Unavailable!;
            Assert.That((rig.Client.State.Stage, unavailable.Kind), Is.EqualTo((ClientStage.MatchUnavailable, MatchUnavailableKind.ConnectionGaveUp)));
            Assert.That(unavailable.GiveUp, Is.EqualTo(GiveUpReason.TokenRefusedRepeatedly(3)));
        }

        [Test]
        public async Task TentarDeNovoForaDeIndisponivelNaoSeAplica()
        {
            await rig.ReachInMatchAsync();

            StageRequestResult result = rig.Client.RetryMatch();

            Assert.That((result.Applied, result.Stage, rig.Client.State.Stage), Is.EqualTo((false, ClientStage.InMatch, ClientStage.InMatch)));
        }

        [Test]
        public async Task VoltarDeIndisponivelLevaALogado()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            rig.ReceiveMatchFound(catalogStatus: 500);

            StageRequestResult result = rig.Client.ReturnToLobby();

            Assert.That((result.Applied, rig.Client.State.Stage, rig.Client.State.Unavailable), Is.EqualTo((true, ClientStage.SignedIn, (MatchUnavailable?)null)));
        }

        private void RefuseLatestToken(bool firstIsOpen)
        {
            if (!firstIsOpen)
                rig.OpenLatest();

            rig.Sockets.Latest.SimulateText(AuthDeniedFrame);
            rig.Sockets.Latest.SimulateClosed(4001, "auth_denied");
            rig.Pump();
        }
    }
}
