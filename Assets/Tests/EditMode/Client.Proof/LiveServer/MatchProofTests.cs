#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Anathema.Net.Unity;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>
    /// A prova final da camada de rede (specs/005-presentation-facade/contracts/match-proof.md; quickstart §3): dois
    /// clientes headless compostos como o jogo compõe jogam duas partidas contra o backend local usando só a fachada. O
    /// teste só bombeia os dois clientes a cada quadro e espera o roteiro. Substitui o marco <c>LiveMatchTests</c> da 004.
    /// </summary>
    [Explicit, Category("LiveServer")]
    public class MatchProofTests
    {
        // Folga sobre o tempo limite do roteiro, para a falha vir dele, com passo e estado dos clientes, e não daqui.
        private static readonly TimeSpan PumpLimit = TimeSpan.FromMinutes(16);

        [UnityTest]
        public IEnumerator DoisClientesJogamDuasPartidasSoPelaFachada()
        {
            MatchProofScript script = new MatchProofScript(new ProofSetup(LocalServerRoutes.Create(), ClientComposition.CreateConsoleLog()));
            Task running = script.RunAsync();
            Stopwatch pumped = Stopwatch.StartNew();
            try
            {
                while (!running.IsCompleted && pumped.Elapsed < PumpLimit)
                {
                    script.Pump();
                    yield return null;
                }
            }
            finally
            {
                script.Dispose();
            }

            Assert.That(running.IsCompleted, Is.True, $"proof did not finish: is the backend up on {LocalServerRoutes.HostAndPort}?");
            if (running.IsFaulted)
                ExceptionDispatchInfo.Capture(running.Exception!.InnerException!).Throw();
        }
    }
}
#endif
