#nullable enable
using System;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Um passo do roteiro da prova que não se cumpriu: o número do passo, o que se esperava e o que chegou, com o estágio,
    /// a rodada e a fase de cada cliente (specs/005-presentation-facade/contracts/match-proof.md, "Montagem").
    /// </summary>
    /// <example>
    /// <code>
    /// catch (ProofFailure failure)
    /// {
    ///     log.Error("proof_failed", new LogField("step", failure.Step), new LogField("received", failure.Received));
    /// }
    /// </code>
    /// </example>
    public sealed class ProofFailure : Exception
    {
        /// <summary>Falha do passo com o esperado e o recebido.</summary>
        /// <example><code>throw new ProofFailure("7", "QueueRefusal DeckNotFound", "no refusal");</code></example>
        public ProofFailure(string step, string expected, string received)
            : base($"proof step {step} failed: expected {expected}, received {received}")
        {
            Step = step;
            Expected = expected;
            Received = received;
        }

        /// <summary>O número do passo em contracts/match-proof.md.</summary>
        /// <example><code>string step = failure.Step; // "12"</code></example>
        public string Step { get; }

        /// <summary>O que o passo esperava.</summary>
        /// <example><code>string expected = failure.Expected;</code></example>
        public string Expected { get; }

        /// <summary>O que chegou, com o estado dos dois clientes.</summary>
        /// <example><code>string received = failure.Received;</code></example>
        public string Received { get; }
    }
}
