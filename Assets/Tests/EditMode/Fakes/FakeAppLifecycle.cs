#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Ciclo de vida roteirizado. A duração fora sai do <see cref="FakeMonotonicClock"/>,
    /// então o teste avança o relógio entre a ida e a volta. Ordem errada lança.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeAppLifecycle lifecycle = new FakeAppLifecycle(clock);
    /// lifecycle.SimulateBackground();
    /// clock.Advance(TimeSpan.FromMinutes(7));
    /// lifecycle.SimulateForeground();
    /// </code>
    /// </example>
    public sealed class FakeAppLifecycle : IAppLifecycle
    {
        private readonly FakeMonotonicClock clock;
        private bool inBackground;
        private MonotonicInstant leftAt;

        /// <summary>Cria o ciclo de vida sobre o relógio falso do teste.</summary>
        /// <example><code>FakeAppLifecycle lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());</code></example>
        public FakeAppLifecycle(FakeMonotonicClock clock)
        {
            this.clock = clock;
        }

        /// <summary>O app saiu do primeiro plano.</summary>
        public event Action<WentToBackground>? WentToBackground;

        /// <summary>O app voltou.</summary>
        public event Action<ReturnedToForeground>? ReturnedToForeground;

        /// <summary>Simula a ida para o segundo plano.</summary>
        /// <example><code>lifecycle.SimulateBackground();</code></example>
        public void SimulateBackground()
        {
            if (inBackground)
                throw new InvalidOperationException("FakeAppLifecycle.SimulateBackground while already in background: expected SimulateForeground first");

            inBackground = true;
            leftAt = clock.Now;
            WentToBackground?.Invoke(new WentToBackground(leftAt));
        }

        /// <summary>Simula a volta, com a duração lida do relógio.</summary>
        /// <example><code>lifecycle.SimulateForeground();</code></example>
        public void SimulateForeground()
        {
            if (!inBackground)
                throw new InvalidOperationException("FakeAppLifecycle.SimulateForeground while in foreground: expected SimulateBackground first");

            inBackground = false;
            MonotonicInstant now = clock.Now;
            ReturnedToForeground?.Invoke(new ReturnedToForeground(now, now - leftAt));
        }
    }
}
