#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// Item de <c>cards</c> com <c>card_type</c> fora do conjunto do contrato. O leitor o omite e
    /// registra; o resto do catálogo continua valendo (FR-031).
    /// </summary>
    /// <example><code>CatalogEntry entry = new UnrecognizedCatalogCard("relic");</code></example>
    internal sealed class UnrecognizedCatalogCard : CatalogEntry
    {
        private readonly string cardTypeText;

        /// <summary>Guarda o texto do tipo desconhecido.</summary>
        internal UnrecognizedCatalogCard(string cardTypeText)
        {
            this.cardTypeText = cardTypeText;
        }

        internal override CatalogCard? Card => null;

        internal override string CardTypeText => cardTypeText;
    }
}
