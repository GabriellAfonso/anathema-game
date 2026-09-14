#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Uma renovação por vez (FR-013): quem pede durante uma renovação em curso recebe a mesma
    /// tarefa. O resultado só é aplicado se a sessão ainda é a mesma geração em que começou
    /// (FR-016); quem esperava recebe o resultado de qualquer forma.
    /// </summary>
    /// <example>
    /// <code>
    /// RenewalOutcome outcome = await session.Renewal.RenewAsync();
    /// </code>
    /// </example>
    internal sealed class TokenRenewal
    {
        private readonly AccountSession session;
        private readonly RefreshCall refreshCall;
        private readonly IClientLog log;
        private Task<RenewalOutcome>? inFlight;

        /// <summary>Cria a renovação da sessão.</summary>
        /// <example><code>TokenRenewal renewal = new TokenRenewal(session, refreshCall, log);</code></example>
        internal TokenRenewal(AccountSession session, RefreshCall refreshCall, IClientLog log)
        {
            this.session = session;
            this.refreshCall = refreshCall;
            this.log = log;
        }

        /// <summary>Renova, ou devolve a renovação em curso.</summary>
        /// <example><code>RenewalOutcome outcome = await renewal.RenewAsync();</code></example>
        internal Task<RenewalOutcome> RenewAsync()
        {
            if (inFlight != null)
                return inFlight;

            SessionTokens? current = session.CurrentTokens;
            if (current == null)
                return Task.FromResult(session.UnavailableKind == SessionUnavailableKind.Expired ? RenewalOutcome.SessionExpired() : RenewalOutcome.NoSession());

            Task<RenewalOutcome> running = RunAsync(current.Refresh, session.Generation);
            // Com transporte que completa na hora, RunAsync já terminou aqui: guardar a tarefa
            // concluída faria a próxima renovação devolver o resultado velho.
            inFlight = running.IsCompleted ? null : running;
            return running;
        }

        private async Task<RenewalOutcome> RunAsync(RefreshToken refresh, int generation)
        {
            RenewalOutcome outcome;
            try
            {
                outcome = await refreshCall.SendAsync(refresh).ConfigureAwait(false);
            }
            finally
            {
                inFlight = null;
            }

            Apply(outcome, generation);
            return outcome;
        }

        private void Apply(RenewalOutcome outcome, int generation)
        {
            if (outcome.Kind == RenewalOutcomeKind.SessionExpired)
            {
                session.Expire(generation);
                return;
            }

            if (outcome.Kind == RenewalOutcomeKind.Unavailable)
            {
                log.Warning("access_token_renewal_unavailable", new LogField("reason", outcome.Reason.ToString()), new LogField("detail", outcome.Detail));
                return;
            }

            if (session.ApplyRenewedAccess(generation, outcome.Token!))
                log.Info("access_token_renewed", new LogField("user_id", outcome.Token!.Owner.Value));
        }
    }
}
