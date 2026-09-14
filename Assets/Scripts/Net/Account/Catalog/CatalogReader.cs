#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê <c>cards</c> tolerando carta ruim (FR-030, FR-031; research R8): cada item é decodificado
    /// sozinho; tipo desconhecido, campo ausente ou de tipo errado e <c>card_id</c> repetido omitem
    /// só aquele item, com registro. Sem <c>cards</c>, a leitura inteira falha.
    /// </summary>
    /// <example>
    /// <code>
    /// IReadOnlyList&lt;CatalogCard&gt; cards = new CatalogReader(log).Read(body);
    /// </code>
    /// </example>
    internal sealed class CatalogReader
    {
        private readonly IClientLog log;
        private readonly DiscriminatedUnion<CatalogEntry> entries;

        /// <summary>Cria o leitor com os braços <c>unit</c> e <c>spell</c>.</summary>
        internal CatalogReader(IClientLog log)
        {
            this.log = log;
            entries = new DiscriminatedUnion<CatalogEntry>("card_type", (cardType, _) => new UnrecognizedCatalogCard(cardType))
                .Register("unit", item => new ListedCatalogCard("unit", UnitCard.Read(item)))
                .Register("spell", item => new ListedCatalogCard("spell", SpellCard.Read(item)));
        }

        /// <summary>As cartas legíveis, na ordem; <c>cards</c> ausente lança <see cref="PayloadShapeException"/>.</summary>
        internal IReadOnlyList<CatalogCard> Read(IPayloadReader body)
        {
            List<CatalogCard> cards = new List<CatalogCard>();
            HashSet<CardId> seen = new HashSet<CardId>();
            foreach (IPayloadReader item in body.ReadObjectList("cards"))
                Keep(ReadItem(item), cards, seen);

            return cards;
        }

        private CatalogCard? ReadItem(IPayloadReader item)
        {
            DecodeOutcome<CatalogEntry> entry = entries.DecodeObject(item);
            if (!entry.IsValid)
            {
                LogSkipped(item, "invalid_field", new LogField("field", entry.Failure.Path));
                return null;
            }

            if (entry.Value.Card == null)
                LogSkipped(item, "unknown_card_type", new LogField("card_type", entry.Value.CardTypeText));
            else
                LogUnknownEffectValues(entry.Value.Card);

            return entry.Value.Card;
        }

        private void Keep(CatalogCard? card, List<CatalogCard> cards, HashSet<CardId> seen)
        {
            if (card == null)
                return;

            if (seen.Add(card.Card))
                cards.Add(card);
            else
                log.Warning("catalog_card_duplicated", new LogField("card_id", card.Card.Value));
        }

        private void LogUnknownEffectValues(CatalogCard card)
        {
            if (!(card is SpellCard spell))
                return;

            if (spell.Effect.TargetKind == SpellTargetKind.Unknown)
                LogUnknownValue(spell, "target_kind", spell.Effect.TargetKindText);

            if (spell.Effect.Duration == SpellDuration.Unknown)
                LogUnknownValue(spell, "duration", spell.Effect.DurationText);
        }

        private void LogUnknownValue(SpellCard spell, string field, string value)
        {
            log.Warning("catalog_effect_value_unknown", new LogField("card_id", spell.Card.Value), new LogField("field", field), new LogField("value", value));
        }

        private void LogSkipped(IPayloadReader item, string reason, LogField detail)
        {
            log.Warning("catalog_card_skipped", new LogField("card_id", CardIdForLog(item)), new LogField("reason", reason), detail);
        }

        private static string CardIdForLog(IPayloadReader item)
        {
            try
            {
                return item.ReadOptionalInteger("card_id")?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing";
            }
            catch (PayloadShapeException)
            {
                return "unreadable";
            }
        }
    }
}
