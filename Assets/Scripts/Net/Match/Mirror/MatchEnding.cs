#nullable enable
using System;

namespace Anathema.Net.Match
{
    /// <summary>Aviso de partida terminou: o desfecho e se o próprio jogador venceu. Sai uma vez por partida.</summary>
    /// <example>
    /// <code>
    /// mirror.MatchEnded.Subscribe(ending => ShowResult(ending.Won ? "Vitória" : "Derrota"));
    /// </code>
    /// </example>
    public sealed class MatchEnding
    {
        /// <summary>Aviso com o desfecho e o resultado para o próprio jogador.</summary>
        /// <example><code>MatchEnding ending = new MatchEnding(outcome, true);</code></example>
        public MatchEnding(MatchOutcome outcome, bool won)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome), $"ending outcome is null (won={won}): expected the outcome of the finished view");
            Won = won;
        }

        /// <summary>Quem perdeu e por quê.</summary>
        /// <example><code>MatchOutcome outcome = ending.Outcome;</code></example>
        public MatchOutcome Outcome { get; }

        /// <summary>O próprio jogador venceu: o derrotado é o outro.</summary>
        /// <example><code>bool won = ending.Won;</code></example>
        public bool Won { get; }
    }
}
