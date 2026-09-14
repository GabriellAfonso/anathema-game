#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>Aviso de fase mudou entre dois frames aceitos. Não sai no primeiro frame: não há fase anterior.</summary>
    /// <example>
    /// <code>
    /// mirror.PhaseChanged += change => { if (change.Current == MatchPhase.Combat) ShowDefense(); };
    /// </code>
    /// </example>
    public sealed class PhaseChange
    {
        /// <summary>Aviso com a fase de antes e a de agora.</summary>
        /// <example><code>PhaseChange change = new PhaseChange(MatchPhase.Action, MatchPhase.Declaration);</code></example>
        public PhaseChange(MatchPhase previous, MatchPhase current)
        {
            Previous = previous;
            Current = current;
        }

        /// <summary>A fase de antes.</summary>
        /// <example><code>MatchPhase before = change.Previous;</code></example>
        public MatchPhase Previous { get; }

        /// <summary>A fase de agora.</summary>
        /// <example><code>MatchPhase now = change.Current;</code></example>
        public MatchPhase Current { get; }
    }
}
