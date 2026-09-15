#nullable enable
using System;
using Anathema.Net.Connection;

namespace Anathema.Net.Match
{
    /// <summary>O estado da sessão de partida, imutável: fase, marca de desatualizado, desfecho ou motivo de desistência.</summary>
    /// <example>
    /// <code>
    /// match.StatusChanged.Subscribe(status => { if (status.Phase == LiveMatchPhase.Finished) ShowResult(status.Outcome); });
    /// </code>
    /// </example>
    public sealed class LiveMatchStatus : IEquatable<LiveMatchStatus>
    {
        private LiveMatchStatus(LiveMatchPhase phase, bool isStale, MatchOutcome? outcome, GiveUpReason? giveUp)
        {
            Phase = phase;
            IsStale = isStale;
            Outcome = outcome;
            GiveUp = giveUp;
        }

        /// <summary>A fase.</summary>
        /// <example><code>bool live = status.Phase == LiveMatchPhase.Live;</code></example>
        public LiveMatchPhase Phase { get; }

        /// <summary>O estado espelhado pode estar atrás do servidor (reconectando ou desistida).</summary>
        /// <example><code>staleOverlay.SetActive(status.IsStale);</code></example>
        public bool IsStale { get; }

        /// <summary>O desfecho, em <see cref="LiveMatchPhase.Finished"/>.</summary>
        /// <example><code>MatchOutcome? outcome = status.Outcome;</code></example>
        public MatchOutcome? Outcome { get; }

        /// <summary>O motivo, em <see cref="LiveMatchPhase.Refused"/> e <see cref="LiveMatchPhase.GaveUp"/>.</summary>
        /// <example><code>string text = status.GiveUp?.PlayerText() ?? string.Empty;</code></example>
        public GiveUpReason? GiveUp { get; }

        /// <summary>Estado sem dado: ocioso, conectando ou ao vivo.</summary>
        /// <example><code>LiveMatchStatus live = LiveMatchStatus.Of(LiveMatchPhase.Live);</code></example>
        public static LiveMatchStatus Of(LiveMatchPhase phase)
        {
            if (phase != LiveMatchPhase.Idle && phase != LiveMatchPhase.Connecting && phase != LiveMatchPhase.Live)
                throw new ArgumentException($"phase is {phase}: expected Idle, Connecting or Live; use Reconnecting(), FinishedWith, RefusedBy or GivenUp", nameof(phase));

            return new LiveMatchStatus(phase, false, null, null);
        }

        /// <summary>Reconectando, com o estado marcado como desatualizado.</summary>
        /// <example><code>LiveMatchStatus status = LiveMatchStatus.Reconnecting();</code></example>
        public static LiveMatchStatus Reconnecting() => new LiveMatchStatus(LiveMatchPhase.Reconnecting, true, null, null);

        /// <summary>Terminada, com o desfecho da visão final.</summary>
        /// <example><code>LiveMatchStatus status = LiveMatchStatus.FinishedWith(view.Outcome);</code></example>
        public static LiveMatchStatus FinishedWith(MatchOutcome? outcome) => new LiveMatchStatus(LiveMatchPhase.Finished, false, outcome, null);

        /// <summary>Recusada pelo servidor; mantém a marca de desatualizado de antes.</summary>
        /// <example><code>LiveMatchStatus status = LiveMatchStatus.RefusedBy(reason, false);</code></example>
        public static LiveMatchStatus RefusedBy(GiveUpReason reason, bool isStale) => new LiveMatchStatus(LiveMatchPhase.Refused, isStale, null, Require(reason));

        /// <summary>A conexão desistiu por outro motivo; o estado espelhado fica desatualizado.</summary>
        /// <example><code>LiveMatchStatus status = LiveMatchStatus.GivenUp(reason);</code></example>
        public static LiveMatchStatus GivenUp(GiveUpReason reason) => new LiveMatchStatus(LiveMatchPhase.GaveUp, true, null, Require(reason));

        /// <summary>Mesma fase, marca, desfecho e motivo.</summary>
        /// <example><code>bool same = status.Equals(other);</code></example>
        public bool Equals(LiveMatchStatus? other)
        {
            return other != null && Phase == other.Phase && IsStale == other.IsStale && ReferenceEquals(Outcome, other.Outcome) && Equals(GiveUp, other.GiveUp);
        }

        /// <summary>Igualdade por valor.</summary>
        /// <example><code>bool same = status.Equals((object)other);</code></example>
        public override bool Equals(object? obj) => Equals(obj as LiveMatchStatus);

        /// <summary>Hash da fase e da marca.</summary>
        /// <example><code>int hash = status.GetHashCode();</code></example>
        public override int GetHashCode() => ((int)Phase * 397) ^ (IsStale ? 1 : 0);

        /// <summary>Fase, marca e motivo, para log.</summary>
        /// <example><code>string text = status.ToString(); // Reconnecting stale=true</code></example>
        public override string ToString() => $"{Phase} stale={(IsStale ? "true" : "false")}{(GiveUp == null ? string.Empty : " " + GiveUp)}";

        private static GiveUpReason Require(GiveUpReason reason)
        {
            return reason ?? throw new ArgumentNullException(nameof(reason), "give up reason is null: expected the reason from the connection status");
        }
    }
}
