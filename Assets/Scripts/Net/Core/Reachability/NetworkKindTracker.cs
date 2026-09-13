#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Decide quando consultar a rede e transforma leituras em mudanças. A consulta é
    /// espaçada em 1 s porque, no Android, cada leitura de alcançabilidade vai ao sistema
    /// (specs/001-server-connection/research.md, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// if (tracker.ShouldPoll()) tracker.Observe(NetworkKindMapping.From(Application.internetReachability));
    /// </code>
    /// </example>
    public sealed class NetworkKindTracker
    {
        /// <summary>Intervalo mínimo entre consultas.</summary>
        /// <example><code>TimeSpan every = NetworkKindTracker.PollInterval;</code></example>
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        private readonly IMonotonicClock clock;
        private MonotonicInstant lastPollAt;

        /// <summary>Cria o rastreador com o tipo de rede lido agora.</summary>
        /// <example><code>NetworkKindTracker tracker = new NetworkKindTracker(clock, NetworkKind.LocalArea);</code></example>
        public NetworkKindTracker(IMonotonicClock clock, NetworkKind initial)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "clock is null: expected the monotonic clock that spaces the polls");
            Current = initial;
            lastPollAt = clock.Now;
        }

        /// <summary>Tipo de rede da última leitura.</summary>
        /// <example><code>NetworkKind now = tracker.Current;</code></example>
        public NetworkKind Current { get; private set; }

        /// <summary>A rede mudou.</summary>
        public event Action<NetworkKindChanged>? Changed;

        /// <summary>Verdadeiro quando já passou o intervalo; marca a consulta.</summary>
        /// <example><code>if (!tracker.ShouldPoll()) return;</code></example>
        public bool ShouldPoll()
        {
            MonotonicInstant now = clock.Now;
            if (now - lastPollAt < PollInterval)
                return false;

            lastPollAt = now;
            return true;
        }

        /// <summary>Registra uma leitura; avisa só se mudou.</summary>
        /// <example><code>tracker.Observe(NetworkKind.CarrierData);</code></example>
        public void Observe(NetworkKind observed)
        {
            if (observed == Current)
                return;

            NetworkKind previous = Current;
            Current = observed;
            Changed?.Invoke(new NetworkKindChanged(previous, observed));
        }
    }
}
