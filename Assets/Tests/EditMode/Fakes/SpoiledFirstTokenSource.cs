#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Embrulha uma fonte de token real e estraga só o primeiro token válido, para o servidor recusá-lo.
    /// Prova contra o backend local que a conexão renova e conecta
    /// (specs/003-authenticated-socket-queue/research.md, R14).
    /// </summary>
    /// <example>
    /// <code>
    /// IAccessTokenSource tokens = new SpoiledFirstTokenSource(account.Tokens);
    /// </code>
    /// </example>
    internal sealed class SpoiledFirstTokenSource : IAccessTokenSource
    {
        private const string SpoilPrefix = "spoiled.";

        private readonly IAccessTokenSource inner;
        private bool spoiled;

        /// <summary>Fonte que estraga o primeiro token de <paramref name="inner"/>.</summary>
        /// <example><code>IAccessTokenSource tokens = new SpoiledFirstTokenSource(account.Tokens);</code></example>
        public SpoiledFirstTokenSource(IAccessTokenSource inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner), "inner token source is null: expected the account session's token source");
        }

        /// <inheritdoc />
        public async Task<AccessTokenOutcome> GetValidAsync()
        {
            AccessTokenOutcome outcome = await inner.GetValidAsync();
            if (spoiled || outcome.Kind != AccessTokenOutcomeKind.Valid)
                return outcome;

            spoiled = true;
            AccessToken real = outcome.Token!;
            return AccessTokenOutcome.Valid(new AccessToken(SpoilPrefix + real.RevealForRequest(), real.Owner, real.Lifetime, real.ArrivedAt));
        }

        /// <inheritdoc />
        public Task<RenewalOutcome> RenewNowAsync() => inner.RenewNowAsync();
    }
}
