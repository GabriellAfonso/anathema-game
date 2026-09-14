#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Corpos e respostas das rotas de conta, que não têm contrato escrito no backend; formas
    /// verificadas no código. O login do projeto (<c>server/apps/accounts/views.py</c>) devolve
    /// <c>token</c> e <c>refresh</c>; o <c>TokenRefreshView</c> do SimpleJWT devolve <c>access</c>.
    /// Falha de leitura nunca ecoa o corpo, que carrega tokens.
    /// </summary>
    /// <example>
    /// <code>
    /// string body = codec.EncodeObject(writer => AccountRequestBodies.WriteLogin(writer, username, password));
    /// </code>
    /// </example>
    internal static class AccountRequestBodies
    {
        private static readonly IReadOnlyDictionary<string, string> JsonHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json",
        };

        /// <summary>POST com corpo JSON, sem autenticação.</summary>
        /// <example><code>HttpRequestSpec request = AccountRequestBodies.JsonPost(routes.Login, body);</code></example>
        internal static HttpRequestSpec JsonPost(Uri url, string body) => new HttpRequestSpec("POST", url, JsonHeaders, body);

        /// <summary>Corpo do login.</summary>
        /// <example><code>AccountRequestBodies.WriteLogin(writer, "one", password);</code></example>
        internal static void WriteLogin(IPayloadWriter writer, string username, Password password)
        {
            writer.WriteText("username", username);
            writer.WriteText("password", password.RevealForRequest());
        }

        /// <summary>Corpo da renovação.</summary>
        /// <example><code>AccountRequestBodies.WriteRefresh(writer, refresh);</code></example>
        internal static void WriteRefresh(IPayloadWriter writer, RefreshToken refresh)
        {
            writer.WriteText("refresh", refresh.RevealForRequest());
        }

        /// <summary>Corpo do cadastro, com os quatro campos do <c>RegisterSerializer</c>.</summary>
        /// <example><code>AccountRequestBodies.WriteRegistration(writer, form);</code></example>
        internal static void WriteRegistration(IPayloadWriter writer, RegistrationForm form)
        {
            writer.WriteText("username", form.Username);
            writer.WriteText("email", form.Email);
            writer.WriteText("password", form.Password.RevealForRequest());
            writer.WriteText("password_confirmation", form.Confirmation.RevealForRequest());
        }

        /// <summary>Tokens da resposta 200 do login.</summary>
        /// <example><code>DecodeOutcome&lt;SessionTokens&gt; issued = AccountRequestBodies.ReadLoginTokens(codec, reader, body, clock.Now);</code></example>
        internal static DecodeOutcome<SessionTokens> ReadLoginTokens(IProtocolCodec codec, AccessTokenReader reader, string body, MonotonicInstant arrivedAt)
        {
            DecodeOutcome<IPayloadReader> json = DecodeQuietly(codec, body);
            if (!json.IsValid)
                return DecodeOutcome<SessionTokens>.Invalid(json.Failure);

            try
            {
                return ReadIssued(json.Value, reader, arrivedAt);
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<SessionTokens>.Invalid(shape.Failure);
            }
        }

        /// <summary>Token de acesso da resposta 200 da renovação.</summary>
        /// <example><code>DecodeOutcome&lt;AccessToken&gt; access = AccountRequestBodies.ReadRefreshedAccess(codec, reader, body, clock.Now);</code></example>
        internal static DecodeOutcome<AccessToken> ReadRefreshedAccess(IProtocolCodec codec, AccessTokenReader reader, string body, MonotonicInstant arrivedAt)
        {
            DecodeOutcome<IPayloadReader> json = DecodeQuietly(codec, body);
            if (!json.IsValid)
                return DecodeOutcome<AccessToken>.Invalid(json.Failure);

            try
            {
                return reader.Read(json.Value.ReadText("access"), arrivedAt);
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<AccessToken>.Invalid(shape.Failure);
            }
        }

        private static DecodeOutcome<SessionTokens> ReadIssued(IPayloadReader body, AccessTokenReader reader, MonotonicInstant arrivedAt)
        {
            DecodeOutcome<AccessToken> access = reader.Read(body.ReadText("token"), arrivedAt);
            if (!access.IsValid)
                return DecodeOutcome<SessionTokens>.Invalid(access.Failure);

            return DecodeOutcome<SessionTokens>.Valid(new SessionTokens(access.Value, ReadRefresh(body)));
        }

        private static RefreshToken ReadRefresh(IPayloadReader body)
        {
            string text = body.ReadText("refresh");
            if (!string.IsNullOrWhiteSpace(text))
                return new RefreshToken(text);

            throw new PayloadShapeException(new DecodeFailure(DecodeFailureKind.InvalidValue, "refresh", "refresh is blank: expected the refresh token issued by the login"));
        }

        private static DecodeOutcome<IPayloadReader> DecodeQuietly(IProtocolCodec codec, string body)
        {
            DecodeOutcome<IPayloadReader> json = codec.DecodeObject(body);
            // O detalhe do codec ecoa o texto recebido; aqui ele pode conter tokens.
            return json.IsValid ? json : DecodeOutcome<IPayloadReader>.Invalid(new DecodeFailure(json.Failure.Kind, string.Empty, "account response body is not a JSON object: expected the fields of the account route"));
        }
    }
}
