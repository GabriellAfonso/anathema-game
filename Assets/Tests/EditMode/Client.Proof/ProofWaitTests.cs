#nullable enable
using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>
    /// A espera do roteiro. Os dois casos terminam sem ceder a vez: no editor, bloquear a thread principal esperando um
    /// <c>Task.Yield</c> travaria o teste.
    /// </summary>
    public class ProofWaitTests
    {
        [Test]
        public void CondicaoCumpridaTerminaNaHora()
        {
            Task waited = new ProofWait(TimeSpan.FromMinutes(1)).UntilAsync(() => true, () => new ProofFailure("1", "never", "never"));

            Assert.That(waited.IsCompleted && !waited.IsFaulted, Is.True);
        }

        [Test]
        public void TempoEsgotadoLancaAFalhaDoPasso()
        {
            Task waited = new ProofWait(TimeSpan.FromTicks(-1)).UntilAsync(() => false, () => new ProofFailure("7", "a refusal", "none"));

            ProofFailure failure = Assert.Throws<ProofFailure>(() => waited.GetAwaiter().GetResult());
            Assert.That((failure.Step, failure.Expected, failure.Received), Is.EqualTo(("7", "a refusal", "none")));
        }
    }
}
