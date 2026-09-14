#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de procurar um <see cref="CardId"/> no catálogo: unidade, feitiço, ou não
    /// encontrada. Carta ausente é resultado explícito, não exceção (FR-029).
    /// </summary>
    /// <example>
    /// <code>
    /// CardLookup lookup = catalog.Find(card);
    /// if (lookup.Kind == CardLookupKind.Spell) ShowSpell(lookup.Spell!);
    /// </code>
    /// </example>
    public sealed class CardLookup
    {
        private CardLookup(CardId requested, CardLookupKind kind, UnitCard? unit, SpellCard? spell)
        {
            Requested = requested;
            Kind = kind;
            Unit = unit;
            Spell = spell;
        }

        /// <summary>O identificador procurado.</summary>
        /// <example><code>CardId asked = lookup.Requested;</code></example>
        public CardId Requested { get; }

        /// <summary>O que foi encontrado.</summary>
        /// <example><code>CardLookupKind kind = lookup.Kind;</code></example>
        public CardLookupKind Kind { get; }

        /// <summary>A unidade; só em <see cref="CardLookupKind.Unit"/>.</summary>
        /// <example><code>long? attack = lookup.Unit?.Attack;</code></example>
        public UnitCard? Unit { get; }

        /// <summary>O feitiço; só em <see cref="CardLookupKind.Spell"/>.</summary>
        /// <example><code>SpellEffect? effect = lookup.Spell?.Effect;</code></example>
        public SpellCard? Spell { get; }

        internal static CardLookup Of(CardId requested, CatalogCard? card)
        {
            if (card is UnitCard unit)
                return new CardLookup(requested, CardLookupKind.Unit, unit, null);

            return card is SpellCard spell ? new CardLookup(requested, CardLookupKind.Spell, null, spell) : new CardLookup(requested, CardLookupKind.NotFound, null, null);
        }
    }
}
