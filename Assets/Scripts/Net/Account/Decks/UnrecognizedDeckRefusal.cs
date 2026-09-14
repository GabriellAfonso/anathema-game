#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>Resposta de deck que não é sucesso e não tem forma reconhecida (ex.: 502 com HTML).</summary>
    /// <example><code>ShowGenericError(unrecognized.Status);</code></example>
    public sealed class UnrecognizedDeckRefusal : DeckRefusalReason
    {
        /// <summary>Cria o motivo com status e trecho do corpo.</summary>
        /// <example><code>DeckRefusalReason reason = new UnrecognizedDeckRefusal(new UnrecognizedRefusal(502, body));</code></example>
        public UnrecognizedDeckRefusal(UnrecognizedRefusal refusal)
        {
            UnrecognizedRefusal required = refusal ?? throw new ArgumentNullException(nameof(refusal), "unrecognized deck refusal is null: expected status and body");
            Status = required.Status;
            BodyText = required.BodyText;
        }

        /// <summary>Status HTTP.</summary>
        /// <example><code>int status = unrecognized.Status;</code></example>
        public int Status { get; }

        /// <summary>Até 500 caracteres do corpo.</summary>
        /// <example><code>string body = unrecognized.BodyText;</code></example>
        public string BodyText { get; }
    }
}
