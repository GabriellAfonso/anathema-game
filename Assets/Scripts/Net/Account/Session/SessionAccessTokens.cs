#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// <see cref="IAccessTokenSource"/> sobre a sessão: devolve o token atual se ele está fora da
    /// margem, senão renova antes (FR-012). A validade vem de <c>exp - iat</c> e do relógio
    /// monotônico, nunca da hora do aparelho (FR-009).
    /// </summary>
    /// <example>
    /// <code>
    /// IAccessTokenSource tokens = new SessionAccessTokens(session, clock, new AccountTiming());
    /// AccessTokenOutcome token = await tokens.GetValidAsync();
    /// </code>
    /// </example>
    public sealed class SessionAccessTokens : IAccessTokenSource
    {
        private readonly AccountSession session;
        private readonly IMonotonicClock clock;
        private readonly AccountTiming timing;

        /// <summary>Cria a porta sobre a sessão, o relógio e a margem.</summary>
        /// <example><code>SessionAccessTokens tokens = new SessionAccessTokens(session, clock, timing);</code></example>
        public SessionAccessTokens(AccountSession session, IMonotonicClock clock, AccountTiming timing)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session), "access token source session is null: expected the account session");
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "access token source clock is null: expected the monotonic clock");
            this.timing = timing ?? throw new ArgumentNullException(nameof(timing), "access token source timing is null: expected the account timing");
        }

        /// <summary>Token válido, renovando antes se faltar menos que a margem.</summary>
        /// <example><code>AccessTokenOutcome token = await tokens.GetValidAsync();</code></example>
        public async Task<AccessTokenOutcome> GetValidAsync()
        {
            AccessToken? current = session.CurrentTokens?.Access;
            if (current == null)
                return AccessTokenOutcome.SessionUnavailable(session.UnavailableKind);

            if (!current.NeedsRenewal(clock.Now, timing.RenewalMargin))
                return AccessTokenOutcome.Valid(current);

            RenewalOutcome renewed = await session.Renewal.RenewAsync().ConfigureAwait(false);
            return AccessTokenOutcome.FromRenewal(renewed);
        }

        /// <summary>Renova agora, sem olhar a margem.</summary>
        /// <example><code>RenewalOutcome renewed = await tokens.RenewNowAsync();</code></example>
        public Task<RenewalOutcome> RenewNowAsync() => session.Renewal.RenewAsync();
    }
}
