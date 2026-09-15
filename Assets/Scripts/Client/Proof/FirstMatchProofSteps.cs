#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using Anathema.Net.Match;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Passos 9 a 14 de contracts/match-proof.md: a partida 1 com os bots, a vez estourada, a queda, o token vencido pelo
    /// salto do relógio, o fim com a linha do histórico e a cobertura dos comandos.
    /// </summary>
    internal static class FirstMatchProofSteps
    {
        private const int RefusalLimit = 20;

        internal static async Task RunAsync(ProofRun run)
        {
            await StartBotsAsync(run, "9", run.Coverage, _ => { }, strategy => strategy.StallFromRound = run.Setup.StallFromRound);
            run.Passed("9", "partida 1 aberta com os dois bots jogando");
            await ExpectTimedOutTurnAsync(run);
            await DropAndRecoverAsync(run);
            await RenewBeforeReopenAsync(run);
            await FinishedResultsAsync(run, "13");
            ExpectBotsBehaved(run, "13");
            run.Passed("13", "os dois veem o mesmo desfecho e a linha do histórico da partida 1");
            ExpectCoverage(run);
        }

        /// <summary>Espera os dois em partida e liga um bot em cada; serve às duas partidas.</summary>
        internal static async Task StartBotsAsync(ProofRun run, string step, CommandCoverage coverage, Action<ProofStrategy> adjustP1, Action<ProofStrategy> adjustP2)
        {
            await run.UntilAsync(step, "both clients InMatch", () => run.AllAt(ClientStage.InMatch));
            run.P1.StartBot(coverage, adjustP1);
            run.P2.StartBot(coverage, adjustP2);
        }

        /// <summary>Espera os dois em partida terminada com a busca da linha encerrada e confere desfecho e linha.</summary>
        internal static async Task<MatchResult> FinishedResultsAsync(ProofRun run, string step)
        {
            await run.UntilAsync(step, "both MatchFinished with the history row lookup done", () => run.Players.All(HasResult));
            MatchResult first = run.P1.Client.State.Result!;
            MatchResult second = run.P2.Client.State.Result!;
            run.Expect(step, first.RowStatus == HistoryRowStatus.Resolved && second.RowStatus == HistoryRowStatus.Resolved, "history rows Resolved", $"{first.RowStatus} and {second.RowStatus}");
            run.Expect(step, SameOutcome(first, second) && first.Won != second.Won, "the same DefeatedUser and Reason with one winner", OutcomeText(first) + " / " + OutcomeText(second));
            run.Expect(step, first.Match == second.Match && first.Row!.Match == first.Match && second.Row!.Match == second.Match, "rows of the same match", $"{first.Match}, {first.Row?.Match}, {second.Row?.Match}");
            run.Expect(step, run.Players.All(player => player.Results.Any(result => result.RowStatus == HistoryRowStatus.Resolved)), "ResultUpdated with Resolved on both", "a client without the notice");
            return first;
        }

        private static async Task ExpectTimedOutTurnAsync(ProofRun run)
        {
            ProofBot p1 = run.P1.Bot!;
            ProofBot p2 = run.P2.Bot!;
            await run.UntilAsync("10", "turn_timed_out then passed in the same match_update on both mirrors", () => p1.SawTimedOutPass && p2.SawTimedOutPass);
            long turn = p2.TimedOutTurns.Last();
            run.Expect("10", p2.RunningOutTurns.Contains(turn), $"Clock.TurnRunningOut for turn {turn} on P2", "running out turns [" + string.Join(",", p2.RunningOutTurns) + "]");
            run.Passed("10", $"vez {turn} de P2 avisou, estourou e passou");
        }

        private static async Task DropAndRecoverAsync(ProofRun run)
        {
            ProofPlayer p2 = run.P2;
            await run.UntilAsync("11", $"P2 live at round {run.Setup.DropRound} or later", () => RoundOf(p2) >= run.Setup.DropRound && IsLive(p2));
            LiveMatch before = p2.Client.CurrentMatch!;
            long version = before.Mirror.Version ?? 0;
            int stageChanges = p2.StageChangeCount, reconnecting = p2.ReconnectingCount, recovered = p2.RecoveredCount;
            p2.Composed.DropSockets();
            await run.UntilAsync("11", "P2 Health.Reconnecting then Recovered", () => p2.ReconnectingCount > reconnecting && p2.RecoveredCount > recovered);
            bool sameMatch = ReferenceEquals(p2.Client.CurrentMatch, before) && p2.Client.State.Stage == ClientStage.InMatch;
            run.Expect("11", sameMatch && p2.StageChangeCount == stageChanges, "stage InMatch without a change and the same CurrentMatch", $"{p2.StageChangeCount - stageChanges} stage changes");
            await run.UntilAsync("11", $"P2 mirror version above {version}", () => (before.Mirror.Version ?? 0) > version);
            run.Passed("11", $"queda de P2 na rodada {RoundOf(p2)} voltou com o estado atual");
        }

        private static async Task RenewBeforeReopenAsync(ProofRun run)
        {
            ProofPlayer p1 = run.P1, p2 = run.P2;
            await run.UntilAsync("12", "a P1 turn with P2 live and not stalled", () => (p1.Client.CurrentMatch?.Mirror.IsMyPriority ?? false) && IsLive(p2) && !p2.Bot!.Strategy.IsStalling);
            TimeSpan jump = run.Setup.ClockJump;
            int mark = p2.Log.Mark("proof_clock_jump", new LogField("player", p2.Label), new LogField("forward_ms", (long)jump.TotalMilliseconds));
            int recovered = p2.RecoveredCount;
            p2.Composed.JumpClock(jump);
            p2.Composed.DropSockets();
            await run.UntilAsync("12", "P2 Health.Recovered after the clock jump", () => p2.RecoveredCount > recovered);
            int renewed = p2.Log.IndexOfFirst("access_token_renewed", mark);
            int opening = p2.Log.IndexOfFirst("connection_opening", mark);
            run.Expect("12", renewed >= 0 && opening > renewed, "access_token_renewed before connection_opening after the jump", $"renewed at {renewed}, opening at {opening}");
            run.Passed("12", "token vencido pelo salto do relógio renovado antes de reabrir");
        }

        private static void ExpectBotsBehaved(ProofRun run, string step)
        {
            foreach (ProofBot bot in run.Players.Select(player => player.Bot!))
            {
                string codes = string.Join(",", bot.Refusals.Select(refusal => refusal.CodeText));
                run.Expect(step, bot.Refusals.Count <= RefusalLimit, $"{bot.Label} refused at most {RefusalLimit} times", $"{bot.Refusals.Count} refusals [{codes}]");
                run.Expect(step, bot.Refusals.All(refusal => refusal.Code != PlayRefusalCode.UnknownMessageType), $"{bot.Label} without unknown_message_type", codes);
                string sent = string.Join(", ", bot.Sent.Select(pair => pair.Key + "=" + pair.Value));
                run.Setup.ConsoleLog.Info("proof_bot", new LogField("player", bot.Label), new LogField("sent", sent), new LogField("refusals", bot.Refusals.Count));
            }
        }

        private static void ExpectCoverage(ProofRun run)
        {
            string missing = string.Join(",", run.Coverage.Missing);
            run.Setup.ConsoleLog.Info("proof_coverage", new LogField("missing", missing));
            run.Expect("14", missing.Length == 0, "every command covered", "missing " + missing);
            run.Passed("14", "os 11 comandos cobertos, mulligan trocando e sem trocar");
        }

        private static bool HasResult(ProofPlayer player)
        {
            ClientState state = player.Client.State;
            return state.Stage == ClientStage.MatchFinished && state.Result != null && state.Result.RowStatus != HistoryRowStatus.Fetching;
        }

        private static bool SameOutcome(MatchResult first, MatchResult second)
        {
            return first.Outcome != null && second.Outcome != null && first.Outcome.DefeatedUser == second.Outcome.DefeatedUser && first.Outcome.Reason == second.Outcome.Reason;
        }

        private static string OutcomeText(MatchResult result)
        {
            return result.Outcome == null ? "no outcome" : $"defeated {result.Outcome.DefeatedUser} by {result.Outcome.Reason}, won={result.Won}";
        }

        private static long RoundOf(ProofPlayer player) => player.Client.CurrentMatch?.Mirror.Current?.RoundNumber ?? 0;

        private static bool IsLive(ProofPlayer player) => player.Client.CurrentMatch?.Status.Phase == LiveMatchPhase.Live;
    }
}
