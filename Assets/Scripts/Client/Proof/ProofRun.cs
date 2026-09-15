#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Uma execução do roteiro, compartilhada pelos blocos de passos: os dois jogadores, a espera com tempo limite, a
    /// cobertura da partida 1 e as linhas do log da prova. Toda falha sai como <see cref="ProofFailure"/> com o estado dos dois.
    /// </summary>
    internal sealed class ProofRun : IDisposable
    {
        internal ProofRun(ProofSetup setup)
        {
            Setup = setup;
            P1 = new ProofPlayer("P1", setup, allowClockJumps: false);
            P2 = new ProofPlayer("P2", setup, allowClockJumps: true);
            Players = new[] { P1, P2 };
            Wait = new ProofWait(setup.Timeout);
        }

        internal ProofSetup Setup { get; }

        internal ProofPlayer P1 { get; }

        internal ProofPlayer P2 { get; }

        internal ProofPlayer[] Players { get; }

        internal ProofWait Wait { get; }

        internal CommandCoverage Coverage { get; } = new CommandCoverage();

        internal void Pump()
        {
            foreach (ProofPlayer player in Players)
                player.Pump();
        }

        internal Task UntilAsync(string step, string expected, Func<bool> condition)
        {
            return Wait.UntilAsync(() =>
            {
                ThrowIfBotFailed(step);
                return condition();
            }, () => new ProofFailure(step, expected, "timeout after " + (long)Wait.Elapsed.TotalSeconds + " s; " + Describe()));
        }

        internal void Expect(string step, bool holds, string expected, string received)
        {
            if (!holds)
                throw new ProofFailure(step, expected, received + "; " + Describe());
        }

        internal bool AllAt(ClientStage stage) => Players.All(player => player.Client.State.Stage == stage);

        internal void Passed(string step, string detail) => new ProofStep(step, true, detail).WriteTo(Setup.ConsoleLog);

        internal void Finish() => WriteFinished(true, "all steps passed");

        internal void Fail(Exception failure)
        {
            new ProofStep(failure is ProofFailure proof ? proof.Step : "?", false, failure.Message).WriteTo(Setup.ConsoleLog);
            WriteFinished(false, failure.Message);
        }

        internal string Describe() => string.Join("; ", Players.Select(player => player.Describe()));

        internal static string ProblemOf<TValue, TRefusal>(AccountCallOutcome<TValue, TRefusal> outcome) where TRefusal : class
        {
            return outcome.Refusal?.ToString() ?? outcome.Failure?.ToString() ?? "success";
        }

        public void Dispose()
        {
            foreach (ProofPlayer player in Players)
                player.Dispose();
        }

        private void ThrowIfBotFailed(string step)
        {
            foreach (ProofPlayer player in Players)
            {
                Exception? failure = player.Bot?.Failure;
                if (failure != null)
                    throw new ProofFailure(step, $"{player.Label} bot always finding a command", failure.Message + "; " + Describe());
            }
        }

        private void WriteFinished(bool passed, string detail)
        {
            Setup.ConsoleLog.Info("proof_finished", new LogField("passed", passed), new LogField("seconds", (long)Wait.Elapsed.TotalSeconds), new LogField("detail", detail));
        }
    }
}
