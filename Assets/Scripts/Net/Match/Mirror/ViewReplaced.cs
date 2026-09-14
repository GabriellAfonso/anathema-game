#nullable enable
using System;

namespace Anathema.Net.Match
{
    /// <summary>Aviso de estado substituído: a visão anterior (nula no primeiro frame) e a atual.</summary>
    /// <example>
    /// <code>
    /// mirror.ViewReplaced += change => Redraw(change.Current);
    /// </code>
    /// </example>
    public sealed class ViewReplaced
    {
        /// <summary>Aviso com a visão anterior e a atual.</summary>
        /// <example><code>ViewReplaced change = new ViewReplaced(null, view);</code></example>
        public ViewReplaced(PlayerView? previous, PlayerView current)
        {
            Previous = previous;
            Current = current ?? throw new ArgumentNullException(nameof(current), "replaced view is null: expected the view just applied");
        }

        /// <summary>A visão de antes; nula no primeiro frame aceito.</summary>
        /// <example><code>bool first = change.Previous == null;</code></example>
        public PlayerView? Previous { get; }

        /// <summary>A visão aplicada agora.</summary>
        /// <example><code>PlayerView view = change.Current;</code></example>
        public PlayerView Current { get; }
    }
}
