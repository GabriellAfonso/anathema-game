#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// Um item de <c>cards</c> depois da união por <c>card_type</c>: uma carta conhecida, ou um tipo
    /// que o cliente não conhece. Existe para a carta desconhecida não precisar fingir os campos de
    /// uma <see cref="CatalogCard"/>; nunca sai do leitor do catálogo.
    /// </summary>
    /// <example><code>CatalogCard? card = entry.Card; // nulo quando o tipo é desconhecido</code></example>
    internal abstract class CatalogEntry
    {
        /// <summary>A carta, ou nulo para tipo desconhecido.</summary>
        internal abstract CatalogCard? Card { get; }

        /// <summary>O texto de <c>card_type</c> como veio.</summary>
        internal abstract string CardTypeText { get; }
    }
}
