#nullable enable
using System;
using System.Threading;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// O relógio da plataforma com um deslocamento que só cresce. Existe para a prova final fazer o token de acesso
    /// vencer sem esperar cinco minutos: o salto é controlado de fora e a composição só o permite com a opção ligada
    /// (specs/005-presentation-facade/research.md, R8). Seguro de qualquer thread, como o relógio de dentro.
    /// </summary>
    internal sealed class SteppableMonotonicClock : IMonotonicClock
    {
        private readonly IMonotonicClock inner;
        private long offsetTicks;

        internal SteppableMonotonicClock(IMonotonicClock inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner), "inner clock is null: expected the platform monotonic clock");
        }

        public MonotonicInstant Now => inner.Now.Add(TimeSpan.FromTicks(Interlocked.Read(ref offsetTicks)));

        internal void Jump(TimeSpan forward)
        {
            if (forward < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(forward), forward, $"clock jump is {forward.TotalMilliseconds} ms: expected zero or forward, a monotonic clock never goes back");

            Interlocked.Add(ref offsetTicks, forward.Ticks);
        }
    }
}
