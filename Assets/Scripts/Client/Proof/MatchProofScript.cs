#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// O roteiro da prova final (specs/005-presentation-facade/contracts/match-proof.md): dois clientes compostos como o
    /// jogo compõe, usando só a superfície da fachada, cadastram, jogam duas partidas contra o servidor e conferem o
    /// histórico. Quem bombeia é o chamador: o teste chama <see cref="Pump"/> a cada quadro; no aparelho,
    /// <see cref="ProofSetup.Attach"/> liga cada cliente a um objeto de cena.
    /// </summary>
    /// <example>
    /// <code>
    /// MatchProofScript script = new MatchProofScript(new ProofSetup(LocalServerRoutes.Create(), log));
    /// Task running = script.RunAsync();
    /// while (!running.IsCompleted) { script.Pump(); yield return null; }
    /// script.Dispose();
    /// </code>
    /// </example>
    public sealed class MatchProofScript : IDisposable
    {
        private readonly ProofRun run;

        /// <summary>Compõe os dois clientes; o roteiro só começa em <see cref="RunAsync"/>.</summary>
        /// <example><code>MatchProofScript script = new MatchProofScript(setup);</code></example>
        public MatchProofScript(ProofSetup setup)
        {
            run = new ProofRun(setup ?? throw new ArgumentNullException(nameof(setup), "proof setup is null: expected new ProofSetup(routes, consoleLog)"));
        }

        /// <summary>O passo de um quadro dos dois clientes. Chame na thread principal quando nada liga os clientes a uma cena.</summary>
        /// <example><code>script.Pump();</code></example>
        public void Pump() => run.Pump();

        /// <summary>Roda os passos 1 a 18; falha com <see cref="ProofFailure"/> no primeiro passo que não se cumpre.</summary>
        /// <example><code>await script.RunAsync();</code></example>
        public async Task RunAsync()
        {
            try
            {
                await AccountProofSteps.RunAsync(run);
                MatchId first = await QueueProofSteps.RunAsync(run);
                await FirstMatchProofSteps.RunAsync(run);
                await SecondMatchProofSteps.RunAsync(run, first);
                run.Finish();
            }
            catch (Exception failure)
            {
                run.Fail(failure);
                throw;
            }
        }

        /// <summary>Para os bots, sai das duas contas (apagando as guardas) e descarta os clientes.</summary>
        /// <example><code>script.Dispose();</code></example>
        public void Dispose() => run.Dispose();
    }
}
