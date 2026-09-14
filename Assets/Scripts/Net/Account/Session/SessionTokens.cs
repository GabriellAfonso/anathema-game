#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// O par de tokens da sessão, só em memória. O refresh não é rotacionado pelo servidor
    /// (<c>ROTATE_REFRESH_TOKENS: False</c>), então a renovação troca só o acesso.
    /// </summary>
    /// <example><code>SessionTokens renewed = current.WithAccess(newAccess);</code></example>
    internal sealed class SessionTokens
    {
        /// <summary>Cria o par emitido pelo login ou pela retomada.</summary>
        /// <example><code>SessionTokens issued = new SessionTokens(access, refresh);</code></example>
        internal SessionTokens(AccessToken access, Anathema.Net.Core.RefreshToken refresh)
        {
            Access = access;
            Refresh = refresh;
        }

        /// <summary>Token de acesso atual.</summary>
        internal AccessToken Access { get; }

        /// <summary>Refresh token, o mesmo desde o login.</summary>
        internal Anathema.Net.Core.RefreshToken Refresh { get; }

        /// <summary>O mesmo par com o acesso trocado.</summary>
        /// <example><code>tokens = tokens.WithAccess(renewed.Token!);</code></example>
        internal SessionTokens WithAccess(AccessToken access) => new SessionTokens(access, Refresh);
    }
}
