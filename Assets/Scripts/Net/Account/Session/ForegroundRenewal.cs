#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Renova na volta ao primeiro plano, antes de alguém precisar (FR-017). No Android o token de
    /// 5 minutos vence com o app minimizado; renovar na volta evita que o primeiro pedido tome 401.
    /// </summary>
    /// <example>
    /// <code>
    /// ForegroundRenewal onReturn = new ForegroundRenewal(lifecycle, session, tokens, clock, timing, log);
    /// onReturn.Dispose(); // ao destruir a camada
    /// </code>
    /// </example>
    public sealed class ForegroundRenewal : IDisposable
    {
        private readonly IAppLifecycle lifecycle;
        private readonly AccountSession session;
        private readonly IAccessTokenSource tokens;
        private readonly IMonotonicClock clock;
        private readonly AccountTiming timing;
        private readonly IClientLog log;

        /// <summary>Assina a volta ao primeiro plano.</summary>
        /// <example><code>ForegroundRenewal onReturn = new ForegroundRenewal(lifecycle, session, tokens, clock, timing, log);</code></example>
        public ForegroundRenewal(IAppLifecycle lifecycle, AccountSession session, IAccessTokenSource tokens, IMonotonicClock clock, AccountTiming timing, IClientLog log)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle), "foreground renewal lifecycle is null: expected the app lifecycle port");
            this.session = session ?? throw new ArgumentNullException(nameof(session), "foreground renewal session is null: expected the account session");
            this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens), "foreground renewal tokens is null: expected the access token source");
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "foreground renewal clock is null: expected the monotonic clock");
            this.timing = timing ?? throw new ArgumentNullException(nameof(timing), "foreground renewal timing is null: expected the account timing");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "foreground renewal log is null: expected the client log");
            lifecycle.ReturnedToForeground += OnReturned;
        }

        /// <summary>Cancela a assinatura.</summary>
        /// <example><code>onReturn.Dispose();</code></example>
        public void Dispose() => lifecycle.ReturnedToForeground -= OnReturned;

        private void OnReturned(ReturnedToForeground signal)
        {
            AccessToken? current = session.CurrentTokens?.Access;
            if (current == null || !current.NeedsRenewal(clock.Now, timing.RenewalMargin))
                return;

            log.Info("access_token_renewal_on_foreground", new LogField("away_ms", (long)signal.AwayFor.TotalMilliseconds));
            _ = ObserveAsync(tokens.GetValidAsync());
        }

        private async Task ObserveAsync(Task<AccessTokenOutcome> renewal)
        {
            // Ninguém espera esta tarefa; sem observar, uma exceção sumiria em silêncio.
            try
            {
                await renewal.ConfigureAwait(false);
            }
            catch (Exception unexpected)
            {
                log.Error("access_token_renewal_on_foreground_failed", new LogField("error", unexpected.GetType().Name));
            }
        }
    }
}
