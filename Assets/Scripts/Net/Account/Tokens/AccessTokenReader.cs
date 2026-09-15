#nullable enable
using System;
using System.Globalization;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê <c>exp</c>, <c>iat</c> e <c>user_id</c> de um JWT sem biblioteca e sem validar a
    /// assinatura: o cliente não tem a chave e não é a autoridade; quem recusa token é o
    /// servidor (research R1). No SimpleJWT 5.5.1, <c>RefreshToken.for_user</c> grava
    /// <c>str(user_id)</c>, então o claim chega como texto (<c>"7"</c>). Nenhuma mensagem de
    /// falha ecoa o token.
    /// </summary>
    /// <example>
    /// <code>
    /// DecodeOutcome&lt;AccessToken&gt; token = new AccessTokenReader(codec).Read(body.ReadText("access"), clock.Now);
    /// </code>
    /// </example>
    internal sealed class AccessTokenReader
    {
        private static readonly long MaxLifetimeSeconds = TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerSecond;
        private readonly IProtocolCodec codec;

        /// <summary>Cria o leitor sobre o codec do projeto.</summary>
        /// <example><code>AccessTokenReader reader = new AccessTokenReader(codec);</code></example>
        public AccessTokenReader(IProtocolCodec codec)
        {
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec), "access token reader codec is null: expected the project codec");
        }

        /// <summary>Lê o token; qualquer forma errada vira falha, nunca exceção.</summary>
        /// <example><code>DecodeOutcome&lt;AccessToken&gt; token = reader.Read(jwt, clock.Now);</code></example>
        public DecodeOutcome<AccessToken> Read(string jwt, MonotonicInstant arrivedAt)
        {
            DecodeOutcome<IPayloadReader> claims = ReadClaims(jwt ?? string.Empty);
            if (!claims.IsValid)
                return DecodeOutcome<AccessToken>.Invalid(claims.Failure);

            try
            {
                return DecodeOutcome<AccessToken>.Valid(Build(jwt!, claims.Value, arrivedAt));
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<AccessToken>.Invalid(shape.Failure);
            }
        }

        private DecodeOutcome<IPayloadReader> ReadClaims(string jwt)
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
                return Invalid(DecodeFailureKind.NotJson, $"token has {parts.Length} dot-separated parts: expected header.payload.signature");

            if (!Base64UrlText.TryDecode(parts[1], out string claimsJson, out string problem))
                return Invalid(DecodeFailureKind.NotJson, $"token payload: {problem}");

            DecodeOutcome<IPayloadReader> decoded = codec.DecodeObject(claimsJson);
            // A falha do codec ecoa o texto recebido; aqui ele é pedaço do token e não pode ir para log.
            return decoded.IsValid ? decoded : Invalid(decoded.Failure.Kind, "token payload is not a JSON object: expected the JWT claims");
        }

        private static AccessToken Build(string jwt, IPayloadReader claims, MonotonicInstant arrivedAt)
        {
            long issuedAt = claims.ReadInteger("iat");
            long expiresAt = claims.ReadInteger("exp");
            long lifetimeSeconds = unchecked(expiresAt - issuedAt);
            if (lifetimeSeconds <= 0 || lifetimeSeconds > MaxLifetimeSeconds)
                throw Shape("exp", $"exp - iat is {lifetimeSeconds}s (exp {expiresAt}, iat {issuedAt}): expected exp after iat");

            return new AccessToken(jwt, ReadOwner(claims), TimeSpan.FromSeconds(lifetimeSeconds), arrivedAt);
        }

        private static UserId ReadOwner(IPayloadReader claims)
        {
            string raw = claims.ReadText("user_id");
            if (long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out long value) && value >= 1)
                return new UserId(value);

            throw Shape("user_id", $"user_id is '{raw}': expected text with a positive integer, as SimpleJWT writes str(user_id)");
        }

        private static PayloadShapeException Shape(string field, string detail)
        {
            return new PayloadShapeException(new DecodeFailure(DecodeFailureKind.InvalidValue, field, detail));
        }

        private static DecodeOutcome<IPayloadReader> Invalid(DecodeFailureKind kind, string detail)
        {
            return DecodeOutcome<IPayloadReader>.Invalid(new DecodeFailure(kind, string.Empty, detail));
        }
    }
}
