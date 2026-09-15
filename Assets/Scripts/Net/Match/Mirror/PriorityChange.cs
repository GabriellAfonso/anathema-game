#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>Aviso de prioridade mudou entre dois frames aceitos; nula no mulligan. Não sai no primeiro frame.</summary>
    /// <example>
    /// <code>
    /// mirror.PriorityChanged.Subscribe(change => highlight.enabled = change.Current == mirror.Self);
    /// </code>
    /// </example>
    public sealed class PriorityChange
    {
        /// <summary>Aviso com a prioridade de antes e a de agora.</summary>
        /// <example><code>PriorityChange change = new PriorityChange(new UserId(9), new UserId(7));</code></example>
        public PriorityChange(UserId? previous, UserId? current)
        {
            Previous = previous;
            Current = current;
        }

        /// <summary>A prioridade de antes.</summary>
        /// <example><code>UserId? before = change.Previous;</code></example>
        public UserId? Previous { get; }

        /// <summary>A prioridade de agora.</summary>
        /// <example><code>UserId? now = change.Current;</code></example>
        public UserId? Current { get; }
    }
}
