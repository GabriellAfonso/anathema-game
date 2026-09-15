#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O relógio que vem em <c>match_start</c> e <c>match_update</c>
    /// (<c>backend/specs/010-match-timers/contracts/server_frames.md</c>, <c>ClockView</c>). Lido como veio:
    /// o cliente não confere a coerência entre a vez e o prazo do mulligan.
    /// </summary>
    /// <example>
    /// <code>
    /// ClockView clock = ClockView.ReadOptional(payload);
    /// bool mulliganRunning = clock.MulliganRemainingMs.HasValue;
    /// </code>
    /// </example>
    public sealed class ClockView
    {
        /// <summary>Relógio sem vez e sem mulligan: frame anterior à feature 010.</summary>
        /// <example><code>ClockView none = ClockView.Empty;</code></example>
        public static readonly ClockView Empty = new ClockView(null, null);

        /// <summary>Relógio com a vez e o prazo do mulligan, cada um nulo quando não vale.</summary>
        /// <example><code>ClockView clock = new ClockView(null, 30000);</code></example>
        public ClockView(TurnView? turn, long? mulliganRemainingMs)
        {
            Turn = turn;
            MulliganRemainingMs = mulliganRemainingMs;
        }

        /// <summary>A vez; nula no mulligan e na partida terminada.</summary>
        /// <example><code>TurnView? turn = clock.Turn;</code></example>
        public TurnView? Turn { get; }

        /// <summary>Milissegundos do próprio mulligan; nulo fora dele ou depois de responder.</summary>
        /// <example><code>long? left = clock.MulliganRemainingMs;</code></example>
        public long? MulliganRemainingMs { get; }

        /// <summary>Lê o campo <c>clock</c> do payload; ausente ou nulo vira <see cref="Empty"/>.</summary>
        /// <example><code>ClockView clock = ClockView.ReadOptional(payload);</code></example>
        internal static ClockView ReadOptional(IPayloadReader payload)
        {
            IPayloadReader? clock = payload.ReadOptionalObject("clock");
            if (clock == null)
                return Empty;

            IPayloadReader? turn = clock.ReadOptionalObject("turn");
            return new ClockView(turn == null ? null : TurnView.Read(turn), clock.ReadOptionalInteger("mulligan_remaining_ms"));
        }
    }
}
