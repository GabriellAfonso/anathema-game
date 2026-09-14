#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Criar exige nome e lista (chave <c>detail</c> do 400 da criação).</summary>
    /// <example><code>ShowError(missing.Detail);</code></example>
    public sealed class MissingDeckField : DeckRefusalReason
    {
        /// <summary>Cria o motivo com o texto do servidor.</summary>
        /// <example><code>DeckRefusalReason reason = new MissingDeckField(body.ReadText("detail"));</code></example>
        public MissingDeckField(string detail)
        {
            Detail = detail;
        }

        /// <summary>Texto do servidor.</summary>
        /// <example><code>string detail = missing.Detail;</code></example>
        public string Detail { get; }
    }
}
