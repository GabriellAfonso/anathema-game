#nullable enable
using System.Globalization;

namespace Anathema.Net.Account.Tests
{
    /// <summary>
    /// Itens de catálogo na forma de <c>http_catalog.md</c>, para os testes montarem respostas sem
    /// repetir o JSON inteiro. Só os campos; a ordem e a lista ficam com cada teste.
    /// </summary>
    internal static class CatalogJson
    {
        internal static string Unit(long cardId, string extraOrReplacement = "\"attack\": 7, \"health\": 5")
        {
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"unit\", \"name\": \"JOHN COPPER\", \"energy\": 5, \"image\": \"john_card\", " + extraOrReplacement + "}";
        }

        internal static string Spell(long cardId, string targetKind = "none", string duration = "permanent", bool requiresTarget = false)
        {
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"spell\", \"name\": \"LIFE POTION\", \"energy\": 4, \"image\": \"life_potion\", "
                + "\"description\": \"Você recupera 5 de Nexus.\", \"effect\": {\"requires_target\": " + (requiresTarget ? "true" : "false")
                + ", \"target_kind\": \"" + targetKind + "\", \"duration\": \"" + duration + "\", \"declaration_only\": false}}";
        }

        internal static string Body(params string[] items) => "{\"cards\": [" + string.Join(", ", items) + "]}";

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
