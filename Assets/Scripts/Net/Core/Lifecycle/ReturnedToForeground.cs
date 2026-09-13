#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O app voltou ao primeiro plano, com quanto tempo ficou fora. A duração conta
    /// o tempo com o aparelho dormindo: é ela que diz se o token de 5 minutos
    /// venceu (specs/001-server-connection/research.md, R1).
    /// </summary>
    /// <example>
    /// <code>
    /// lifecycle.ReturnedToForeground += signal =>
    /// {
    ///     if (signal.AwayFor > TimeSpan.FromMinutes(4)) RenewTokenBeforeReconnect();
    /// };
    /// </code>
    /// </example>
    public sealed class ReturnedToForeground
    {
        /// <summary>Cria o aviso; duração negativa lança.</summary>
        /// <example><code>ReturnedToForeground signal = new ReturnedToForeground(now, now - leftAt);</code></example>
        public ReturnedToForeground(MonotonicInstant at, TimeSpan awayFor)
        {
            if (awayFor < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(awayFor), awayFor, $"time away is {awayFor}: expected a non-negative duration from a monotonic clock");

            At = at;
            AwayFor = awayFor;
        }

        /// <summary>Instante da volta.</summary>
        /// <example><code>MonotonicInstant returnedAt = signal.At;</code></example>
        public MonotonicInstant At { get; }

        /// <summary>Quanto tempo o app ficou fora.</summary>
        /// <example><code>TimeSpan away = signal.AwayFor;</code></example>
        public TimeSpan AwayFor { get; }
    }
}
