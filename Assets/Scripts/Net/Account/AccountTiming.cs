#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Tempos da sessão de conta. A margem de renovação cobre a latência entre a emissão e a
    /// chegada do token (a vida útil contada a partir da chegada fica maior que a real por esse
    /// tanto) e o intervalo entre pedir o token e usá-lo no socket (research R1).
    /// </summary>
    /// <example>
    /// <code>
    /// AccountTiming timing = new AccountTiming(TimeSpan.FromSeconds(30));
    /// </code>
    /// </example>
    internal sealed class AccountTiming
    {
        /// <summary>Margem padrão: 30 s.</summary>
        /// <example><code>TimeSpan margin = AccountTiming.DefaultRenewalMargin;</code></example>
        public static readonly TimeSpan DefaultRenewalMargin = TimeSpan.FromSeconds(30);

        /// <summary>Maior margem aceita: 120 s, menos da metade dos 5 min do token.</summary>
        /// <example><code>TimeSpan limit = AccountTiming.MaxRenewalMargin;</code></example>
        public static readonly TimeSpan MaxRenewalMargin = TimeSpan.FromSeconds(120);

        /// <summary>Tempos com a margem padrão.</summary>
        /// <example><code>AccountTiming timing = new AccountTiming();</code></example>
        public AccountTiming()
            : this(DefaultRenewalMargin)
        {
        }

        /// <summary>Tempos com margem própria, de 0 a 120 s.</summary>
        /// <example><code>AccountTiming timing = new AccountTiming(TimeSpan.Zero);</code></example>
        public AccountTiming(TimeSpan renewalMargin)
        {
            if (renewalMargin < TimeSpan.Zero || renewalMargin > MaxRenewalMargin)
                throw new ArgumentOutOfRangeException(nameof(renewalMargin), renewalMargin, $"renewal margin is {renewalMargin.TotalSeconds}s: expected 0 to {MaxRenewalMargin.TotalSeconds} seconds");

            RenewalMargin = renewalMargin;
        }

        /// <summary>Quanto antes de vencer o token é renovado.</summary>
        /// <example><code>bool renew = token.NeedsRenewal(clock.Now, timing.RenewalMargin);</code></example>
        public TimeSpan RenewalMargin { get; }
    }
}
