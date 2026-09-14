#nullable enable
using System;
using System.Globalization;
using System.Text;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// JWTs de teste com a forma do SimpleJWT 5.5.1: claims <c>token_type</c>, <c>exp</c>,
    /// <c>iat</c>, <c>jti</c> e <c>user_id</c> como texto. A assinatura é falsa porque o cliente
    /// não a valida (specs/002-player-account/research.md, R1). Todo teste monta token por aqui.
    /// </summary>
    /// <example>
    /// <code>
    /// string access = FakeAccessJwt.FiveMinutes("renewed");
    /// </code>
    /// </example>
    public static class FakeAccessJwt
    {
        private const string Header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";

        /// <summary>Token com vida <c>expiresAt - issuedAt</c>; <paramref name="tokenId"/> distingue tokens iguais no resto.</summary>
        /// <example><code>string jwt = FakeAccessJwt.Create(1000, 1300, "7");</code></example>
        public static string Create(long issuedAt, long expiresAt, string userIdClaim = "7", string tokenId = "jti-1")
        {
            string payload = "{\"token_type\":\"access\",\"exp\":" + Number(expiresAt) + ",\"iat\":" + Number(issuedAt)
                + ",\"jti\":\"" + tokenId + "\",\"user_id\":\"" + userIdClaim + "\"}";
            return CreateRaw(payload);
        }

        /// <summary>Token de 5 minutos, a vida padrão do SimpleJWT.</summary>
        /// <example><code>string jwt = FakeAccessJwt.FiveMinutes("login");</code></example>
        public static string FiveMinutes(string tokenId = "jti-1", string userIdClaim = "7")
        {
            return Create(1_700_000_000, 1_700_000_300, userIdClaim, tokenId);
        }

        /// <summary>Token com payload arbitrário, para formas erradas.</summary>
        /// <example><code>string jwt = FakeAccessJwt.CreateRaw("{\"exp\": 1}");</code></example>
        public static string CreateRaw(string payloadJson)
        {
            return Encode(Header) + "." + Encode(payloadJson) + ".test-signature";
        }

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
