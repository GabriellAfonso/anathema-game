#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resposta que não é sucesso e que o cliente não sabe tipar (ex.: 502 com página HTML de
    /// proxy). Guarda o status e um trecho do corpo para depuração; nunca é decidida pelo texto.
    /// </summary>
    /// <example>
    /// <code>
    /// UnrecognizedRefusal refusal = new UnrecognizedRefusal(502, response.BodyText);
    /// </code>
    /// </example>
    public sealed class UnrecognizedRefusal
    {
        /// <summary>Tamanho máximo do trecho de corpo guardado.</summary>
        /// <example><code>int limit = UnrecognizedRefusal.MaxBodyLength;</code></example>
        public const int MaxBodyLength = 500;

        /// <summary>Cria a recusa, cortando o corpo em <see cref="MaxBodyLength"/> caracteres.</summary>
        /// <example><code>UnrecognizedRefusal refusal = new UnrecognizedRefusal(500, "");</code></example>
        public UnrecognizedRefusal(int status, string bodyText)
        {
            string body = bodyText ?? throw new ArgumentNullException(nameof(bodyText), $"body of unrecognized http {status} is null: expected text, empty when there was none");
            Status = status;
            BodyText = body.Length <= MaxBodyLength ? body : body.Substring(0, MaxBodyLength);
        }

        /// <summary>Status HTTP.</summary>
        /// <example><code>int status = refusal.Status;</code></example>
        public int Status { get; }

        /// <summary>Até 500 caracteres do corpo.</summary>
        /// <example><code>string body = refusal.BodyText;</code></example>
        public string BodyText { get; }

        /// <summary>Forma para log, só com o status.</summary>
        /// <example><code>string text = refusal.ToString(); // unrecognized http 502</code></example>
        public override string ToString() => $"unrecognized http {Status}";
    }
}
