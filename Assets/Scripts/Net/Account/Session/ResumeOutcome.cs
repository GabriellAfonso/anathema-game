#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de <see cref="AccountSession.ResumeAsync"/>. Os quatro desfechos são
    /// distinguíveis (FR-019): retomada, nada guardado, recusada, indisponível.
    /// </summary>
    /// <example>
    /// <code>
    /// ResumeOutcome resumed = await session.ResumeAsync();
    /// if (resumed.Kind == ResumeOutcomeKind.Unavailable) ShowRetry();
    /// </code>
    /// </example>
    public sealed class ResumeOutcome
    {
        private ResumeOutcome(ResumeOutcomeKind kind, UserId? user = null, RenewalOutcome? renewal = null)
        {
            Kind = kind;
            User = user;
            Renewal = renewal;
        }

        /// <summary>Como terminou.</summary>
        /// <example><code>ResumeOutcomeKind kind = resumed.Kind;</code></example>
        public ResumeOutcomeKind Kind { get; }

        /// <summary>Quem entrou; só em <see cref="ResumeOutcomeKind.Resumed"/>.</summary>
        /// <example><code>UserId? self = resumed.User;</code></example>
        public UserId? User { get; }

        /// <summary>A renovação que não saiu; só em <see cref="ResumeOutcomeKind.Unavailable"/>.</summary>
        /// <example><code>RenewalUnavailableReason? reason = resumed.Renewal?.Reason;</code></example>
        internal RenewalOutcome? Renewal { get; }

        /// <summary>Entrou sem senha.</summary>
        /// <example><code>return ResumeOutcome.Resumed(token.Owner);</code></example>
        public static ResumeOutcome Resumed(UserId user) => new ResumeOutcome(ResumeOutcomeKind.Resumed, user: user);

        /// <summary>Nada a retomar.</summary>
        /// <example><code>return ResumeOutcome.NothingStored();</code></example>
        public static ResumeOutcome NothingStored() => new ResumeOutcome(ResumeOutcomeKind.NothingStored);

        /// <summary>Refresh recusado.</summary>
        /// <example><code>return ResumeOutcome.Refused();</code></example>
        public static ResumeOutcome Refused() => new ResumeOutcome(ResumeOutcomeKind.Refused);

        /// <summary>Servidor inalcançável ou resposta inesperada.</summary>
        /// <example><code>return ResumeOutcome.Unavailable(renewal);</code></example>
        internal static ResumeOutcome Unavailable(RenewalOutcome renewal)
        {
            return new ResumeOutcome(ResumeOutcomeKind.Unavailable, renewal: renewal ?? throw new ArgumentNullException(nameof(renewal), "unavailable resume renewal is null: expected the renewal outcome"));
        }

        /// <summary>Forma para log, sem token.</summary>
        /// <example><code>string text = resumed.ToString(); // Resumed user_id=7</code></example>
        public override string ToString()
        {
            if (Kind == ResumeOutcomeKind.Resumed)
                return $"{Kind} {User}";

            return Renewal == null ? Kind.ToString() : $"{Kind} {Renewal}";
        }
    }
}
