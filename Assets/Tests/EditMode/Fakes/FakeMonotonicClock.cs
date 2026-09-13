#nullable enable
using System;
using System.Threading;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Relógio que só anda quando o teste manda. Começa longe do zero para que um
    /// cálculo que esqueça a origem apareça no teste. Thread-safe, porque testes
    /// de fila leem de várias threads.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeMonotonicClock clock = new FakeMonotonicClock();
    /// clock.Advance(TimeSpan.FromMinutes(7));
    /// </code>
    /// </example>
    public sealed class FakeMonotonicClock : IMonotonicClock
    {
        /// <summary>Ticks do instante inicial: uma hora depois da origem.</summary>
        /// <example><code>MonotonicInstant start = new MonotonicInstant(FakeMonotonicClock.StartTicks);</code></example>
        public static readonly long StartTicks = TimeSpan.FromHours(1).Ticks;

        private long ticks = StartTicks;

        /// <summary>Instante atual do relógio falso.</summary>
        /// <example><code>MonotonicInstant now = clock.Now;</code></example>
        public MonotonicInstant Now => new MonotonicInstant(Interlocked.Read(ref ticks));

        /// <summary>Avança o relógio; duração negativa é erro de roteiro.</summary>
        /// <example><code>clock.Advance(TimeSpan.FromSeconds(1));</code></example>
        public void Advance(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, $"advance is {duration}: expected a non-negative duration, a monotonic clock never goes back");

            Interlocked.Add(ref ticks, duration.Ticks);
        }
    }
}
