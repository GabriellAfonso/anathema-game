#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Facade;

namespace Anathema.Client.Proof
{
    /// <summary>Passos 7 e 8 de contracts/match-proof.md: recusa de deck inexistente e pareamento com o deck de feitiços.</summary>
    internal static class QueueProofSteps
    {
        // Um deck_id que nenhuma conta nova tem: o servidor recusa com deck_not_found antes de enfileirar.
        private static readonly DeckId MissingDeck = new DeckId(987654321);

        internal static async Task<MatchId> RunAsync(ProofRun run)
        {
            await RefuseMissingDeckAsync(run);
            MatchId match = await PairAsync(run, "8");
            run.Passed("8", "os dois pareados na mesma partida, " + match);
            return match;
        }

        /// <summary>Os dois entram na fila com o deck de feitiços e esperam o mesmo pareamento; serve às duas partidas.</summary>
        internal static async Task<MatchId> PairAsync(ProofRun run, string step)
        {
            int[] before = run.Players.Select(player => player.Pairings.Count).ToArray();
            foreach (ProofPlayer player in run.Players)
            {
                QueueJoinResult joined = player.Client.Queue.Join(player.SpellDeck);
                run.Expect(step, joined.Kind == QueueJoinKind.Started, $"{player.Label} queue join started", joined.ToString());
            }

            await run.UntilAsync(step, "both clients paired", () => run.P1.Pairings.Count > before[0] && run.P2.Pairings.Count > before[1]);
            MatchId first = run.P1.Pairings.Last().Match;
            MatchId second = run.P2.Pairings.Last().Match;
            run.Expect(step, first == second, "the same match_id for both", $"{first} and {second}");
            return first;
        }

        private static async Task RefuseMissingDeckAsync(ProofRun run)
        {
            ProofPlayer p1 = run.P1;
            QueueJoinResult joined = p1.Client.Queue.Join(MissingDeck);
            run.Expect("7", joined.Kind == QueueJoinKind.Started, "queue join started", joined.ToString());
            await run.UntilAsync("7", "QueueRefusal DeckNotFound and back to SignedIn",
                () => p1.QueueRefusals.Any(refusal => refusal.Kind == QueueRefusalKind.DeckNotFound) && p1.Client.State.Stage == ClientStage.SignedIn);
            run.Passed("7", "fila recusou o deck_id inexistente com deck_not_found");
        }
    }
}
