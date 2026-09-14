#nullable enable

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Corpos de resposta das rotas de conta, na forma verificada no backend: o login do projeto
    /// devolve <c>token</c> e <c>refresh</c>; o <c>TokenRefreshView</c> do SimpleJWT devolve
    /// <c>access</c>. Para roteirizar o <see cref="FakeHttpTransport"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes(), "refresh-1"));
    /// </code>
    /// </example>
    public static class FakeAccountResponses
    {
        /// <summary>Corpo 200 de <c>POST /accounts/login/</c>.</summary>
        /// <example><code>string body = FakeAccountResponses.Login(access, "refresh-1");</code></example>
        public static string Login(string accessJwt, string refresh)
        {
            return "{\"refresh\": \"" + refresh + "\", \"token\": \"" + accessJwt + "\"}";
        }

        /// <summary>Corpo 200 de <c>POST /accounts/token/refresh/</c>.</summary>
        /// <example><code>string body = FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("renewed"));</code></example>
        public static string Refresh(string accessJwt) => "{\"access\": \"" + accessJwt + "\"}";
    }
}
