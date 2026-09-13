#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Leitura de identificadores tipados a partir do nome do campo, para que
    /// ninguém leia <c>user_id</c> como <c>long</c> solto (constituição, princípio III).
    /// </summary>
    /// <example>
    /// <code>
    /// UserId opponent = payload.ReadObject("opponent").ReadUserId("user_id");
    /// </code>
    /// </example>
    public static class PayloadIdentityReading
    {
        /// <summary>Lê um <see cref="UserId"/>; inteiro menor que 1 é valor inválido.</summary>
        /// <example><code>UserId self = reader.ReadUserId("user_id");</code></example>
        public static UserId ReadUserId(this IPayloadReader reader, string field)
        {
            long raw = reader.ReadInteger(field);
            if (raw < 1)
                throw Invalid(reader, field, $"{field} is {raw}: expected a positive integer");

            return new UserId(raw);
        }

        /// <summary>Lê um <see cref="CardInstanceId"/>; inteiro negativo é valor inválido.</summary>
        /// <example><code>CardInstanceId attacker = reader.ReadCardInstanceId("card_instance_id");</code></example>
        public static CardInstanceId ReadCardInstanceId(this IPayloadReader reader, string field)
        {
            long raw = reader.ReadInteger(field);
            if (raw < 0)
                throw Invalid(reader, field, $"{field} is {raw}: expected a non-negative integer");

            return new CardInstanceId(raw);
        }

        /// <summary>Lê um <see cref="MatchId"/>; texto vazio ou com espaço nas pontas é valor inválido.</summary>
        /// <example><code>MatchId match = reader.ReadMatchId("match_id");</code></example>
        public static MatchId ReadMatchId(this IPayloadReader reader, string field)
        {
            string raw = reader.ReadText(field);
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() != raw)
                throw Invalid(reader, field, $"{field} is '{raw}': expected non-empty text without surrounding spaces");

            return new MatchId(raw);
        }

        /// <summary><see cref="UserId"/> opcional: nulo se ausente ou <c>null</c>.</summary>
        /// <example><code>UserId? defeated = outcome.ReadOptionalUserId("defeated_user_id");</code></example>
        public static UserId? ReadOptionalUserId(this IPayloadReader reader, string field)
        {
            return reader.ReadOptionalInteger(field) == null ? (UserId?)null : reader.ReadUserId(field);
        }

        /// <summary><see cref="CardInstanceId"/> opcional: nulo se ausente ou <c>null</c>.</summary>
        /// <example><code>CardInstanceId? target = spell.ReadOptionalCardInstanceId("target_card_instance_id");</code></example>
        public static CardInstanceId? ReadOptionalCardInstanceId(this IPayloadReader reader, string field)
        {
            return reader.ReadOptionalInteger(field) == null ? (CardInstanceId?)null : reader.ReadCardInstanceId(field);
        }

        /// <summary><see cref="MatchId"/> opcional: nulo se ausente ou <c>null</c>.</summary>
        /// <example><code>MatchId? current = presence.ReadOptionalMatchId("match_id");</code></example>
        public static MatchId? ReadOptionalMatchId(this IPayloadReader reader, string field)
        {
            return reader.ReadOptionalText(field) == null ? (MatchId?)null : reader.ReadMatchId(field);
        }

        private static PayloadShapeException Invalid(IPayloadReader reader, string field, string detail)
        {
            return new PayloadShapeException(new DecodeFailure(DecodeFailureKind.InvalidValue, PayloadPath.Field(reader.Path, field), detail));
        }
    }
}
