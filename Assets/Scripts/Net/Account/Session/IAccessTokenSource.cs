#nullable enable
using System.Threading.Tasks;

namespace Anathema.Net.Account
{
    /// <summary>
    /// A porta "me dê um token de acesso válido". O cliente HTTP autenticado usa no cabeçalho
    /// <c>Authorization</c>; a feature 3 usa no <c>?token=</c> do socket. Chamadas concorrentes
    /// compartilham a mesma renovação (FR-013).
    /// </summary>
    /// <example>
    /// <code>
    /// AccessTokenOutcome token = await tokens.GetValidAsync();
    /// RenewalOutcome afterRejection = await tokens.RenewNowAsync(); // depois de um close 4001
    /// </code>
    /// </example>
    internal interface IAccessTokenSource
    {
        /// <summary>O token atual, ou um renovado antes se faltar menos que a margem.</summary>
        /// <example><code>AccessTokenOutcome token = await tokens.GetValidAsync();</code></example>
        Task<AccessTokenOutcome> GetValidAsync();

        /// <summary>Renova agora, sem olhar a margem: o servidor já recusou o token atual.</summary>
        /// <example><code>RenewalOutcome renewed = await tokens.RenewNowAsync();</code></example>
        Task<RenewalOutcome> RenewNowAsync();
    }
}
