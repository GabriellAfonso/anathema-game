#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um valor tipado ou o motivo de não haver um. Frame inválido é resultado,
    /// nunca exceção que escapa do codec (FR-033).
    /// </summary>
    /// <example>
    /// <code>
    /// DecodeOutcome&lt;ServerFrame&gt; outcome = codec.Decode(text);
    /// if (!outcome.IsValid) { log.Warning("frame_invalid", new LogField("failure", outcome.Failure.ToString())); return; }
    /// Handle(outcome.Value);
    /// </code>
    /// </example>
    public sealed class DecodeOutcome<T>
    {
        private readonly T value;
        private readonly DecodeFailure? failure;

        private DecodeOutcome(T value, DecodeFailure? failure)
        {
            this.value = value;
            this.failure = failure;
        }

        /// <summary>Verdadeiro quando há valor.</summary>
        /// <example><code>if (outcome.IsValid) Handle(outcome.Value);</code></example>
        public bool IsValid => failure == null;

        /// <summary>O valor; ler em resultado inválido lança.</summary>
        /// <example><code>ServerFrame frame = outcome.Value;</code></example>
        public T Value => IsValid
            ? value
            : throw new InvalidOperationException($"decode outcome is invalid ({failure}): expected IsValid before reading Value");

        /// <summary>O motivo; ler em resultado válido lança.</summary>
        /// <example><code>DecodeFailure why = outcome.Failure;</code></example>
        public DecodeFailure Failure => failure ?? throw new InvalidOperationException($"decode outcome is valid ({value}): expected !IsValid before reading Failure");

        /// <summary>Resultado com valor.</summary>
        /// <example><code>return DecodeOutcome&lt;ServerFrame&gt;.Valid(frame);</code></example>
        public static DecodeOutcome<T> Valid(T value) => new DecodeOutcome<T>(value, null);

        /// <summary>Resultado sem valor.</summary>
        /// <example><code>return DecodeOutcome&lt;ServerFrame&gt;.Invalid(shape.Failure);</code></example>
        public static DecodeOutcome<T> Invalid(DecodeFailure failure)
        {
            if (failure == null)
                throw new ArgumentNullException(nameof(failure), "decode failure is null: expected the reason the frame is invalid");

            return new DecodeOutcome<T>(default!, failure);
        }
    }
}
