#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>data-model.md, "ClientState": cada estágio preenche só o que carrega.</summary>
    public class ClientStateTests
    {
        private static readonly UserId Self = new UserId(7);

        private FacadeTestRig rig = null!;
        private MatchPairing pairing = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            pairing = new MatchPairing(new MatchId("match-7"), new PairedPlayer(Self, "one", "default", 1), new PairedPlayer(new UserId(9), "two", "knight", 3));
        }

        [Test]
        public void DeslogadoSoTemMotivo()
        {
            ClientState state = ClientState.SignedOut(SignedOutReason.SessionExpired);

            Assert.That(state.Stage, Is.EqualTo(ClientStage.SignedOut));
            Assert.That(state.SignedOutReason, Is.EqualTo(SignedOutReason.SessionExpired));
            Assert.That((state.Self, state.Pairing, state.Match, state.Result, state.Unavailable), Is.EqualTo(((UserId?)null, (MatchPairing?)null, (LiveMatch?)null, (MatchResult?)null, (MatchUnavailable?)null)));
        }

        [Test]
        public void LogadoEProcurandoSoTemUsuario()
        {
            foreach (ClientState state in new[] { ClientState.SignedIn(Self), ClientState.Searching(Self) })
            {
                Assert.That(state.Self, Is.EqualTo(Self));
                Assert.That((state.SignedOutReason, state.Pairing, state.Match), Is.EqualTo(((SignedOutReason?)null, (MatchPairing?)null, (LiveMatch?)null)));
            }
        }

        [Test]
        public void PareadoTemPareamentoEPartidaSoDepoisDeAbrir()
        {
            ClientState paired = ClientState.Paired(Self, pairing);
            LiveMatch live = rig.NewLiveMatch("match-7");

            ClientState opened = paired.WithOpenedMatch(live);

            Assert.That((paired.Pairing, paired.Match), Is.EqualTo((pairing, (LiveMatch?)null)));
            Assert.That((opened.Stage, opened.Pairing, opened.Match), Is.EqualTo((ClientStage.Paired, pairing, live)));
        }

        [Test]
        public void FimTemPartidaEResultadoETrocaSoOResultado()
        {
            LiveMatch live = rig.NewLiveMatch("match-7");
            MatchResult fetching = MatchResult.Fetching(pairing.Match, null, true);
            ClientState finished = ClientState.MatchFinished(Self, pairing, live, fetching);

            ClientState resolved = finished.WithResult(fetching.Unavailable(4));

            Assert.That((finished.Match, finished.Result), Is.EqualTo((live, fetching)));
            Assert.That((resolved.Stage, resolved.Match, resolved.Result!.RowStatus), Is.EqualTo((ClientStage.MatchFinished, live, HistoryRowStatus.Unavailable)));
        }

        [Test]
        public void IndisponivelTemMotivoESemPartida()
        {
            MatchUnavailable unavailable = MatchUnavailable.CatalogUnavailable(pairing.Match, null);

            ClientState state = ClientState.MatchUnavailable(Self, pairing, unavailable);

            Assert.That((state.Stage, state.Unavailable, state.Match), Is.EqualTo((ClientStage.MatchUnavailable, unavailable, (LiveMatch?)null)));
        }

        [Test]
        public void TrocarPartidaOuResultadoForaDoEstagioLanca()
        {
            ClientState signedIn = ClientState.SignedIn(Self);

            Assert.Throws<InvalidOperationException>(() => signedIn.WithOpenedMatch(rig.NewLiveMatch("match-7")));
            Assert.Throws<InvalidOperationException>(() => signedIn.WithResult(MatchResult.Fetching(pairing.Match, null, false)));
        }

        [Test]
        public void FabricasRecusamNulo()
        {
            Assert.Throws<ArgumentNullException>(() => ClientState.Paired(Self, null!));
            Assert.Throws<ArgumentNullException>(() => ClientState.InMatch(Self, pairing, null!));
            Assert.Throws<ArgumentNullException>(() => new ClientStageChange(ClientState.SignedIn(Self), null!));
        }

        [Test]
        public void MesmoEstagioEMesmosValoresEhOMesmoEstado()
        {
            Assert.That(ClientState.SignedIn(Self).SameAs(ClientState.SignedIn(Self)), Is.True);
            Assert.That(ClientState.SignedIn(Self).SameAs(ClientState.Searching(Self)), Is.False);
            Assert.That(ClientState.Paired(Self, pairing).SameAs(ClientState.Paired(Self, pairing).WithOpenedMatch(rig.NewLiveMatch("match-7"))), Is.False);
        }

        [Test]
        public void ResultadoDePedidoDizSeAplicouEOEstagio()
        {
            Assert.That((StageRequestResult.Done(ClientStage.SignedIn).Applied, StageRequestResult.NotApplicable(ClientStage.InMatch).Applied), Is.EqualTo((true, false)));
            Assert.That(StageRequestResult.NotApplicable(ClientStage.InMatch).ToString(), Is.EqualTo("applied=false stage=InMatch"));
        }
    }
}
