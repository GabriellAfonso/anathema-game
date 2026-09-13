#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Escrita de identificadores tipados como o valor cru do JSON.</summary>
    /// <example>
    /// <code>
    /// writer.WriteCardInstanceId("card_instance_id", chosen);
    /// </code>
    /// </example>
    public static class PayloadIdentityWriting
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
    }
}
