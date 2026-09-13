#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um campo do payload não tem a forma esperada. Lançada pelos métodos de leitura
    /// para que cada braço de união leia em linha reta; capturada por
    /// <see cref="DiscriminatedUnion{TBase}"/> e pelo codec, nunca sai dele.
    /// </summary>
    /// <example>
    /// <code>
    /// throw new PayloadShapeException(new DecodeFailure(DecodeFailureKind.InvalidValue, "payload.user_id", "user_id is 0: expected a positive integer"));
    /// </code>
    /// </example>
    public sealed class PayloadShapeException : Exception
    {
        /// <summary>Cria a exceção com o motivo.</summary>
        /// <example><code>throw new PayloadShapeException(failure);</code></example>
        public PayloadShapeException(DecodeFailure failure)
            : base(failure?.ToString() ?? "payload shape failure is null: expected a DecodeFailure")
        {
            Failure = failure ?? throw new ArgumentNullException(nameof(failure), "payload shape failure is null: expected a DecodeFailure");
        }

        /// <summary>O motivo, com caminho e detalhe.</summary>
        /// <example><code>return DecodeOutcome&lt;ServerFrame&gt;.Invalid(shape.Failure);</code></example>
        public DecodeFailure Failure { get; }
    }
}
