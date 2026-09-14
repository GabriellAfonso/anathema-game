#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>O que a consulta ao catálogo encontrou.</summary>
    /// <example><code>if (lookup.Kind == CardLookupKind.NotFound) ShowPlaceholder(lookup.Requested);</code></example>
    public enum CardLookupKind
    {
        /// <summary>Carta de unidade.</summary>
        Unit,

        /// <summary>Carta de feitiço.</summary>
        Spell,

        /// <summary>O <c>card_id</c> não está no catálogo.</summary>
        NotFound,
    }
}
