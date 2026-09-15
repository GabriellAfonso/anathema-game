#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de pedir um token de acesso válido: o token, ou por que não há um.
    /// </summary>
    /// <example>
    /// <code>
    /// AccessTokenOutcome token = await tokens.GetValidAsync();
    /// if (token.Kind == AccessTokenOutcomeKind.Valid) socket.Open(new Uri(url + "?token=" + token.Token!.RevealForRequest()));
    /// </code>
    /// </example>
    internal sealed class AccessTokenOutcome
    {
        private AccessTokenOutcome(AccessTokenOutcomeKind kind, AccessToken? token = null, SessionUnavailableKind? session = null, RenewalOutcome? renewal = null)
        {
            Kind = kind;
            Token = token;
            Session = session;
            Renewal = renewal;
        }

        /// <summary>O que foi entregue.</summary>
        /// <example><code>AccessTokenOutcomeKind kind = token.Kind;</code></example>
        public AccessTokenOutcomeKind Kind { get; }

        /// <summary>O token; só em <see cref="AccessTokenOutcomeKind.Valid"/>.</summary>
        /// <example><code>AccessToken? access = token.Token;</code></example>
        public AccessToken? Token { get; }

        /// <summary>Por que não há sessão; só em <see cref="AccessTokenOutcomeKind.SessionUnavailable"/>.</summary>
        /// <example><code>SessionUnavailableKind? session = token.Session;</code></example>
        public SessionUnavailableKind? Session { get; }

        /// <summary>A renovação que não saiu; só em <see cref="AccessTokenOutcomeKind.Unavailable"/>.</summary>
        /// <example><code>RenewalUnavailableReason? reason = token.Renewal?.Reason;</code></example>
        public RenewalOutcome? Renewal { get; }

        /// <summary>Token válido.</summary>
        /// <example><code>return AccessTokenOutcome.Valid(current);</code></example>
        public static AccessTokenOutcome Valid(AccessToken token)
        {
            return new AccessTokenOutcome(AccessTokenOutcomeKind.Valid, token: token ?? throw new ArgumentNullException(nameof(token), "valid access token is null: expected the current or renewed token"));
        }

        /// <summary>Sem sessão.</summary>
        /// <example><code>return AccessTokenOutcome.SessionUnavailable(SessionUnavailableKind.NoSession);</code></example>
        public static AccessTokenOutcome SessionUnavailable(SessionUnavailableKind session) => new AccessTokenOutcome(AccessTokenOutcomeKind.SessionUnavailable, session: session);

        /// <summary>A renovação necessária não saiu.</summary>
        /// <example><code>return AccessTokenOutcome.Unavailable(renewal);</code></example>
        public static AccessTokenOutcome Unavailable(RenewalOutcome renewal)
        {
            return new AccessTokenOutcome(AccessTokenOutcomeKind.Unavailable, renewal: renewal ?? throw new ArgumentNullException(nameof(renewal), "unavailable renewal is null: expected the renewal outcome"));
        }

        /// <summary>Forma para log, sem token.</summary>
        /// <example><code>string text = token.ToString(); // SessionUnavailable Expired</code></example>
        public override string ToString()
        {
            if (Kind == AccessTokenOutcomeKind.Valid)
                return $"{Kind} {Token}";

            return Kind == AccessTokenOutcomeKind.SessionUnavailable ? $"{Kind} {Session}" : $"{Kind} {Renewal}";
        }

        internal static AccessTokenOutcome FromRenewal(RenewalOutcome renewal)
        {
            if (renewal.Kind == RenewalOutcomeKind.Renewed)
                return Valid(renewal.Token!);

            if (renewal.Kind == RenewalOutcomeKind.Unavailable)
                return Unavailable(renewal);

            return SessionUnavailable(renewal.Kind == RenewalOutcomeKind.SessionExpired ? SessionUnavailableKind.Expired : SessionUnavailableKind.NoSession);
        }
    }
}
