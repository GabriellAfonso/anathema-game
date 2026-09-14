#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Item de <c>cards</c> com <c>card_type</c> conhecido.</summary>
    /// <example><code>CatalogEntry entry = new ListedCatalogCard("unit", UnitCard.Read(item));</code></example>
    internal sealed class ListedCatalogCard : CatalogEntry
    {
        private readonly CatalogCard card;
        private readonly string cardTypeText;

        /// <summary>Guarda a carta lida e o tipo.</summary>
        internal ListedCatalogCard(string cardTypeText, CatalogCard card)
        {
            this.cardTypeText = cardTypeText;
            this.card = card;
        }

        internal override CatalogCard? Card => card;

        internal override string CardTypeText => cardTypeText;
    }
}
