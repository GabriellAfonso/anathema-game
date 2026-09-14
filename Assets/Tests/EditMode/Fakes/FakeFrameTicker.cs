#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Quadros na mão do teste: cada <see cref="Tick"/> é um <c>Update</c> do hospedeiro. Junto com o
    /// <see cref="FakeMonotonicClock"/>, substitui a espera real.
    /// </summary>
    /// <example>
    /// <code>
    /// clock.Advance(TimeSpan.FromSeconds(1));
    /// ticker.Tick();
    /// </code>
    /// </example>
    public sealed class FakeFrameTicker : IFrameTicker
    {
        /// <inheritdoc />
        public event Action? Ticked;

        /// <summary>Quantos quadros o teste já passou.</summary>
        /// <example><code>Assert.That(ticker.Ticks, Is.EqualTo(3));</code></example>
        public int Ticks { get; private set; }

        /// <summary>Passa um quadro.</summary>
        /// <example><code>ticker.Tick();</code></example>
        public void Tick()
        {
            Ticks++;
            Ticked?.Invoke();
        }
    }
}
