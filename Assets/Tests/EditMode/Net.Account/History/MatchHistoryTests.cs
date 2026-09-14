#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class MatchHistoryTests
    {
        private const string TwoRows = "{\"count\": 37, \"next\": \"http://host/game/matches/?page=2\", \"previous\": null, \"results\": ["
            + "{\"match_id\": \"4f1c2a9e-8d3b-4c77-9a51-6b0e2f7d1c84\", \"won\": true, \"end_reason\": \"nexus_depleted\", "
            + "\"opponent\": {\"user_id\": 9, \"nickname\": \"brenda\", \"icon\": \"default_icon\", \"level\": 3}, "
            + "\"duration_seconds\": 742, \"final_round\": 8, \"ended_at\": \"2026-09-12T18:03:11Z\"}, "
            + "{\"match_id\": \"c07b1d55-2e64-4f0a-b3aa-91d8e4c6f220\", \"won\": false, \"end_reason\": \"forfeit\", \"opponent\": null, "
            + "\"duration_seconds\": 63, \"final_round\": 1, \"ended_at\": \"2026-09-11T22:47:02Z\"}]}";

        [Test]
        public async Task ContaNovaTemPaginaVazia()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{\"count\": 0, \"next\": null, \"previous\": null, \"results\": []}");

            MatchHistoryPage page = (await History(rig).ReadPageAsync(new HistoryPageRequest())).Value;

            Assert.That(page.Count, Is.EqualTo(0));
            Assert.That(page.Rows, Is.Empty);
            Assert.That(page.HasNext, Is.False);
            Assert.That(page.HasPrevious, Is.False);
        }

        [Test]
        public async Task LinhasTrazemDesfechoOponenteEInstanteParaExibir()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, TwoRows);

            MatchHistoryPage page = (await History(rig).ReadPageAsync(new HistoryPageRequest())).Value;

            Assert.That(page.HasNext, Is.True);
            Assert.That(page.Rows[0].Match, Is.EqualTo(new MatchId("4f1c2a9e-8d3b-4c77-9a51-6b0e2f7d1c84")));
            Assert.That(page.Rows[0].Won, Is.True);
            Assert.That(page.Rows[0].EndReason, Is.EqualTo(MatchEndReason.NexusDepleted));
            Assert.That(page.Rows[0].Opponent!.User, Is.EqualTo(new UserId(9)));
            Assert.That(page.Rows[0].DurationSeconds, Is.EqualTo(742));
            Assert.That(page.Rows[0].FinalRound, Is.EqualTo(8));
            Assert.That(page.Rows[0].EndedAtText, Is.EqualTo("2026-09-12T18:03:11Z"));
            Assert.That(page.Rows[1].EndReason, Is.EqualTo(MatchEndReason.Forfeit));
            Assert.That(page.Rows[1].Opponent, Is.Null);
        }

        [Test]
        public async Task MotivoDoFimDesconhecidoMantemALinha()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, TwoRows.Replace("\"forfeit\"", "\"draw\""));

            MatchHistoryPage page = (await History(rig).ReadPageAsync(new HistoryPageRequest())).Value;

            Assert.That(page.Rows[1].EndReason, Is.EqualTo(MatchEndReason.Unknown));
            Assert.That(page.Rows[1].EndReasonText, Is.EqualTo("draw"));
            rig.Log.Single("history_end_reason_unknown");
        }

        [Test]
        public async Task NaoEncontradoAlemDaPrimeiraPaginaEhPassouDoFim()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(404, "{\"detail\": \"Página inválida.\"}");

            AccountCallOutcome<MatchHistoryPage, HistoryRefusal> outcome = await History(rig).ReadPageAsync(new HistoryPageRequest(3));

            Assert.That(outcome.Refusal!.Kind, Is.EqualTo(HistoryRefusalKind.PastTheEnd));
        }

        [Test]
        public async Task NaoEncontradoNaPrimeiraPaginaEhContaSemPerfil()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");

            AccountCallOutcome<MatchHistoryPage, HistoryRefusal> outcome = await History(rig).ReadPageAsync(new HistoryPageRequest(1));

            Assert.That(outcome.Refusal!.Kind, Is.EqualTo(HistoryRefusalKind.NoProfile));
        }

        [Test]
        public async Task OutroStatusEhRecusaNaoReconhecida()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(500, "<html>");

            AccountCallOutcome<MatchHistoryPage, HistoryRefusal> outcome = await History(rig).ReadPageAsync(new HistoryPageRequest());

            Assert.That(outcome.Refusal!.Kind, Is.EqualTo(HistoryRefusalKind.Unrecognized));
            Assert.That(outcome.Refusal.Unrecognized!.Status, Is.EqualTo(500));
        }

        [Test]
        public async Task PedidoLevaPaginaETamanho()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{\"count\": 0, \"next\": null, \"previous\": null, \"results\": []}");

            await History(rig).ReadPageAsync(new HistoryPageRequest(2, 5));

            Assert.That(rig.Http.Requests[1].Url.Query, Is.EqualTo("?page=2&page_size=5"));
        }

        private static MatchHistory History(AccountTestRig rig) => new MatchHistory(rig.Client, rig.Codec, rig.Log, rig.Routes);

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
