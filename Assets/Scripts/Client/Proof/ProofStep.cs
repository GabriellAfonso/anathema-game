#nullable enable
using Anathema.Net.Core;

namespace Anathema.Client.Proof
{
    /// <summary>Um passo do roteiro com o resultado e o detalhe, escrito como a linha <c>proof_step</c> do log da prova.</summary>
    internal sealed class ProofStep
    {
        internal ProofStep(string number, bool passed, string detail)
        {
            Number = number;
            Passed = passed;
            Detail = detail;
        }

        internal string Number { get; }

        internal bool Passed { get; }

        internal string Detail { get; }

        internal void WriteTo(IClientLog log)
        {
            LogField[] fields = { new LogField("step", Number), new LogField("passed", Passed), new LogField("detail", Detail) };
            if (Passed)
                log.Info("proof_step", fields);
            else
                log.Error("proof_step", fields);
        }
    }
}
