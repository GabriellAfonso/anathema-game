#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US5-5, US5-6, US5-7, FR-015 a FR-018, SC-003: a linha do histórico depois do fim.</summary>
    public class HistoryRowLookupTests
    {
        private FacadeTestRig rig = null!;
        private List<MatchResult> updates = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            updates = new List<MatchResult>();
            rig.Client.ResultUpdated.Subscribe(updates.Add);
        }

        [Test]
        public async Task LeiturasEmZeroUmTresESeteSegundosNuncaUmaQuinta()
        {
            await rig.ReachFinishedAsync(Without(), Without(), Without(), Without());
            Assert.That(rig.HistoryReads(), Is.EqualTo(1));

            AssertReadsAfter(TimeSpan.FromMilliseconds(900), 1);
            AssertReadsAfter(TimeSpan.FromMilliseconds(100), 2);
            AssertReadsAfter(TimeSpan.FromMilliseconds(1900), 2);
            AssertReadsAfter(TimeSpan.FromMilliseconds(100), 3);
            AssertReadsAfter(TimeSpan.FromMilliseconds(3900), 3);
            AssertReadsAfter(TimeSpan.FromMilliseconds(100), 4);
            AssertReadsAfter(TimeSpan.FromMinutes(1), 4);

            MatchResult result = rig.Client.State.Result!;
            Assert.That((result.RowStatus, result.Attempts, result.Row, result.Won), Is.EqualTo((HistoryRowStatus.Unavailable, 4, null as Anathema.Net.Account.MatchHistoryRow, true)));
            Assert.That((updates.Count, rig.Client.State.Stage), Is.EqualTo((1, ClientStage.MatchFinished)));
        }

        [Test]
        public async Task LinhaNaPrimeiraLeituraResolveUmaVez()
        {
            await rig.ReachFinishedAsync(FacadeTestRig.HistoryPage(withRow: true));

            MatchResult result = rig.Client.State.Result!;
            Assert.That((result.RowStatus, result.Attempts, result.Row!.FinalRound), Is.EqualTo((HistoryRowStatus.Resolved, 1, 8L)));
            Assert.That(updates, Is.EqualTo(new[] { result }));
            AssertReadsAfter(TimeSpan.FromSeconds(30), 1);
        }

        [Test]
        public async Task LinhaNaTerceiraLeituraResolveComTresLeituras()
        {
            await rig.ReachFinishedAsync(Without(), Without(), FacadeTestRig.HistoryPage(withRow: true));

            rig.Advance(TimeSpan.FromSeconds(1));
            rig.Advance(TimeSpan.FromSeconds(2));

            Assert.That((rig.Client.State.Result!.RowStatus, rig.Client.State.Result.Attempts, updates.Count), Is.EqualTo((HistoryRowStatus.Resolved, 3, 1)));
        }

        [Test]
        public async Task FalhaERecusaContamComoTentativa()
        {
            await rig.ReachInMatchAsync();
            rig.Http.RespondNext(500, "{\"detail\": \"falhou\"}");
            rig.Http.RespondNext(404, "{\"detail\": \"Not found.\"}");
            rig.Http.RespondNext(200, Without());
            rig.Http.RespondNext(200, Without());
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);

            AdvanceThroughAllWaits();

            Assert.That((rig.Client.State.Result!.RowStatus, rig.Client.State.Result.Attempts), Is.EqualTo((HistoryRowStatus.Unavailable, 4)));
        }

        [Test]
        public async Task VoltarAntesDaLinhaCancelaSemAviso()
        {
            await rig.ReachFinishedAsync(Without());

            rig.Client.ReturnToLobby();
            AdvanceThroughAllWaits();

            Assert.That((rig.HistoryReads(), updates.Count), Is.EqualTo((1, 0)));
        }

        [Test]
        public async Task SairOuDescartarAntesDaLinhaCancelaSemAviso()
        {
            await rig.ReachFinishedAsync(Without());

            rig.Client.Account.SignOut();
            AdvanceThroughAllWaits();
            rig.Client.Dispose();

            Assert.That((rig.HistoryReads(), updates.Count), Is.EqualTo((1, 0)));
        }

        [Test]
        public async Task ExpirarAntesDaLinhaCancelaSemAviso()
        {
            await rig.ReachFinishedAsync(Without());

            await rig.ExpireSessionAsync();
            AdvanceThroughAllWaits();

            Assert.That((rig.HistoryReads(), updates.Count, rig.Client.State.Stage), Is.EqualTo((1, 0, ClientStage.SignedOut)));
        }

        [Test]
        public async Task VitoriaDivergenteRegistraENadaEhCorrigido()
        {
            await rig.ReachFinishedAsync(FacadeTestRig.HistoryPage(withRow: true, won: false));

            MatchResult result = rig.Client.State.Result!;
            Assert.That((result.Won, result.Row!.Won), Is.EqualTo((true, false)));
            Assert.That(rig.LogValue("match_history_disagrees", "match_id"), Is.EqualTo(FacadeTestRig.MatchIdText));
        }

        [Test]
        public async Task OponenteApagadoResolveSemOponente()
        {
            await rig.ReachFinishedAsync(FacadeTestRig.HistoryPage(withRow: true, opponent: "null"));

            Assert.That((rig.Client.State.Result!.RowStatus, rig.Client.State.Result.Row!.Opponent), Is.EqualTo((HistoryRowStatus.Resolved, null as Anathema.Net.Account.HistoryOpponent)));
            Assert.That(rig.LogValue("match_history_row", "status"), Is.EqualTo(nameof(HistoryRowStatus.Resolved)));
        }

        private static string Without() => FacadeTestRig.HistoryPage(withRow: false);

        private void AssertReadsAfter(TimeSpan duration, int expected)
        {
            rig.Advance(duration);
            Assert.That(rig.HistoryReads(), Is.EqualTo(expected), $"leituras depois de avançar {duration.TotalMilliseconds} ms");
        }

        private void AdvanceThroughAllWaits()
        {
            foreach (int seconds in new[] { 1, 2, 4, 30 })
                rig.Advance(TimeSpan.FromSeconds(seconds));
        }
    }
}
