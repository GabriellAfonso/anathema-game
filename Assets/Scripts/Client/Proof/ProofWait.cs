#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Espera as condições do roteiro sob um tempo limite total, como o <c>UntilAsync</c> da 004. Não bombeia nada: quem
    /// bombeia os clientes é o chamador (a corrotina do teste ou o <c>NetworkLayerHost</c> do runner), e cada volta cede
    /// com <c>Task.Yield</c> (specs/005-presentation-facade/research.md, R11).
    /// </summary>
    internal sealed class ProofWait
    {
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly TimeSpan limit;

        internal ProofWait(TimeSpan limit)
        {
            this.limit = limit;
        }

        internal TimeSpan Elapsed => elapsed.Elapsed;

        internal async Task UntilAsync(Func<bool> condition, Func<Exception> timedOut)
        {
            while (!condition())
            {
                if (elapsed.Elapsed > limit)
                    throw timedOut();

                await Task.Yield();
            }
        }
    }
}
