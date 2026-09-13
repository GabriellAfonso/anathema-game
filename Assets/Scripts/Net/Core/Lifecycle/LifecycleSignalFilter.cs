#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Transforma os sinais de pausa e foco do motor em ida e volta do segundo
    /// plano. Nenhum dos dois sinais basta sozinho: no Android, Home com o teclado
    /// aberto chama só a pausa; no Windows sem Run In Background, alt-tab em tela
    /// cheia chama só o foco (specs/001-server-connection/research.md, R2).
    /// </summary>
    /// <example>
    /// <code>
    /// LifecycleSignalFilter filter = new LifecycleSignalFilter(clock, focusLossStopsPlayer: !Application.runInBackground);
    /// filter.ReturnedToForeground += signal => log.Info("app_foreground", new LogField("away_ms", (long)signal.AwayFor.TotalMilliseconds));
    /// filter.OnPause(true);
    /// </code>
    /// </example>
    public sealed class LifecycleSignalFilter
    {
        private readonly IMonotonicClock clock;
        private readonly bool focusLossStopsPlayer;
        private bool inBackground;
        private MonotonicInstant leftAt;

        /// <summary>
        /// Cria o filtro. <paramref name="focusLossStopsPlayer"/> é verdadeiro quando
        /// perder o foco para o player (Windows sem Run In Background).
        /// </summary>
        /// <example><code>LifecycleSignalFilter android = new LifecycleSignalFilter(clock, false);</code></example>
        public LifecycleSignalFilter(IMonotonicClock clock, bool focusLossStopsPlayer)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "clock is null: expected the monotonic clock that measures time away");
            this.focusLossStopsPlayer = focusLossStopsPlayer;
        }

        /// <summary>O app saiu do primeiro plano.</summary>
        public event Action<WentToBackground>? WentToBackground;

        /// <summary>O app voltou, com a duração fora.</summary>
        public event Action<ReturnedToForeground>? ReturnedToForeground;

        /// <summary>Sinal de pausa do motor.</summary>
        /// <example><code>filter.OnPause(pauseStatus);</code></example>
        public void OnPause(bool paused)
        {
            if (paused)
                EnterBackground();
            else
                ReturnToForeground();
        }

        /// <summary>Sinal de foco do motor. Perda de foco só conta se para o player.</summary>
        /// <example><code>filter.OnFocus(hasFocus);</code></example>
        public void OnFocus(bool focused)
        {
            if (focused)
            {
                ReturnToForeground();
                return;
            }

            if (focusLossStopsPlayer)
                EnterBackground();
        }

        private void EnterBackground()
        {
            if (inBackground)
                return;

            inBackground = true;
            leftAt = clock.Now;
            WentToBackground?.Invoke(new WentToBackground(leftAt));
        }

        private void ReturnToForeground()
        {
            if (!inBackground)
                return;

            inBackground = false;
            MonotonicInstant now = clock.Now;
            ReturnedToForeground?.Invoke(new ReturnedToForeground(now, now - leftAt));
        }
    }
}
