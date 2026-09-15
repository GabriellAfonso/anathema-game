#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Passos 15 a 18 de contracts/match-proof.md: volta ao lobby, partida 2 com desistência no mulligan, histórico das
    /// duas contas e saída.
    /// </summary>
    internal static class SecondMatchProofSteps
    {
        internal static async Task RunAsync(ProofRun run, MatchId firstMatch)
        {
            await ReturnToLobbyAsync(run);
            MatchId secondMatch = await ForfeitInMulliganAsync(run);
            await ExpectHistoryAsync(run, firstMatch, secondMatch);
            await SignOutAsync(run);
        }

        private static async Task ReturnToLobbyAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                player.StopBot();
                StageRequestResult returned = player.Client.ReturnToLobby();
                run.Expect("15", returned.Applied, $"{player.Label} back to the lobby", returned.ToString());
            }

            await run.UntilAsync("15", "both clients SignedIn", () => run.AllAt(ClientStage.SignedIn));
            run.Passed("15", "os dois de volta a logado");
        }

        private static async Task<MatchId> ForfeitInMulliganAsync(ProofRun run)
        {
            MatchId match = await QueueProofSteps.PairAsync(run, "16");
            await FirstMatchProofSteps.StartBotsAsync(run, "16", new CommandCoverage(), strategy => strategy.ForfeitsInMulligan = true, _ => { });
            MatchResult result = await FirstMatchProofSteps.FinishedResultsAsync(run, "16");
            MatchResult other = run.P2.Client.State.Result!;
            bool forfeited = result.Match == match && result.Outcome!.Reason == MatchEndReason.Forfeit && result.Outcome.DefeatedUser == run.P1.User;
            run.Expect("16", forfeited, $"P1 defeated by Forfeit in {match}", $"{result.Match} defeated {result.Outcome?.DefeatedUser} by {result.Outcome?.Reason}");
            run.Expect("16", result.Row!.FinalRound == 1 && other.Row!.FinalRound == 1, "history rows with FinalRound 1", $"{result.Row.FinalRound} and {other.Row?.FinalRound}");
            run.Passed("16", "partida 2 terminou por desistência no mulligan, rodada final 1");
            return match;
        }

        private static async Task ExpectHistoryAsync(ProofRun run, MatchId firstMatch, MatchId secondMatch)
        {
            Dictionary<string, MatchHistoryPage> pages = new Dictionary<string, MatchHistoryPage>();
            foreach (ProofPlayer player in run.Players)
            {
                AccountCallOutcome<MatchHistoryPage, HistoryRefusal> page = await player.Client.History.ReadPageAsync(new HistoryPageRequest());
                run.Expect("17", page.IsSuccess, $"{player.Label} history page", ProofRun.ProblemOf(page));
                pages[player.Label] = page.Value;
            }

            foreach (MatchId match in new[] { firstMatch, secondMatch })
                ExpectOneWinner(run, match, RowOf(pages[run.P1.Label], match), RowOf(pages[run.P2.Label], match));

            run.Passed("17", "as duas partidas nas duas contas, um vencedor e um perdedor em cada");
        }

        private static void ExpectOneWinner(ProofRun run, MatchId match, MatchHistoryRow? first, MatchHistoryRow? second)
        {
            run.Expect("17", first != null && second != null, $"{match} in both histories", $"P1 row {first != null}, P2 row {second != null}");
            run.Expect("17", first!.Won != second!.Won, $"one winner in {match}", $"won {first.Won} and {second.Won}");
        }

        private static MatchHistoryRow? RowOf(MatchHistoryPage page, MatchId match) => page.Rows.FirstOrDefault(row => row.Match == match);

        private static async Task SignOutAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                StageRequestResult signedOut = player.Client.Account.SignOut();
                run.Expect("18", signedOut.Applied, $"{player.Label} signed out", signedOut.ToString());
            }

            await run.UntilAsync("18", "both clients SignedOut", () => run.AllAt(ClientStage.SignedOut));
            run.Passed("18", "saída dos dois apagou as guardas");
        }
    }
}
