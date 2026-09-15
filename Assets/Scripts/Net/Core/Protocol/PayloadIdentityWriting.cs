#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Anathema.Net.Core
{
    /// <summary>Escrita de identificadores tipados como o valor cru do JSON.</summary>
    /// <example>
    /// <code>
    /// writer.WriteCardInstanceId("card_instance_id", chosen);
    /// </code>
    /// </example>
    internal static class PayloadIdentityWriting
    {
        /// <summary>Escreve o inteiro cru de um <see cref="UserId"/>.</summary>
        /// <example><code>writer.WriteUserId("user_id", self);</code></example>
        public static void WriteUserId(this IPayloadWriter writer, string field, UserId id)
        {
            writer.WriteInteger(field, id.Value);
        }

        /// <summary>Escreve o inteiro cru de um <see cref="CardInstanceId"/>.</summary>
        /// <example><code>writer.WriteCardInstanceId("card_instance_id", attacker);</code></example>
        public static void WriteCardInstanceId(this IPayloadWriter writer, string field, CardInstanceId id)
        {
            writer.WriteInteger(field, id.Value);
        }

        /// <summary>Escreve o texto cru de um <see cref="MatchId"/>.</summary>
        /// <example><code>writer.WriteMatchId("match_id", match);</code></example>
        public static void WriteMatchId(this IPayloadWriter writer, string field, MatchId id)
        {
            writer.WriteText(field, id.Value);
        }

        /// <summary>Escreve o deck como inteiro cru.</summary>
        /// <example><code>writer.WriteDeckId("deck_id", deck);</code></example>
        public static void WriteDeckId(this IPayloadWriter writer, string field, DeckId id)
        {
            writer.WriteInteger(field, id.Value);
        }

        /// <summary>Escreve a lista de cartas como inteiros crus, na ordem dada.</summary>
        /// <example><code>writer.WriteCardIdList("card_ids", draft.Cards);</code></example>
        public static void WriteCardIdList(this IPayloadWriter writer, string field, IReadOnlyList<CardId> cards)
        {
            writer.WriteIntegerList(field, cards.Select(card => card.Value).ToArray());
        }

        /// <summary>Escreve a lista de cópias na partida como inteiros crus, na ordem dada; vazia vira <c>[]</c>.</summary>
        /// <example><code>writer.WriteCardInstanceIdList("attacker_card_instance_ids", attackers);</code></example>
        public static void WriteCardInstanceIdList(this IPayloadWriter writer, string field, IReadOnlyList<CardInstanceId> cards)
        {
            writer.WriteIntegerList(field, cards.Select(card => card.Value).ToArray());
        }
    }
}
