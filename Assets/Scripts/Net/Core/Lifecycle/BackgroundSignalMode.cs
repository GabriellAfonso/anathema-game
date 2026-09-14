#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Como os avisos de pausa e foco da plataforma viram "foi para segundo plano"
    /// (specs/001-server-connection/research.md, R2; specs/003-authenticated-socket-queue/research.md, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// LifecycleSignalFilter filter = new LifecycleSignalFilter(clock, BackgroundSignalMode.DesktopKeepsRunning);
    /// </code>
    /// </example>
    public enum BackgroundSignalMode
    {
        /// <summary>Android: pausa é segundo plano; perda de foco sozinha (teclado, diálogo) não é.</summary>
        AndroidPause,

        /// <summary>Desktop sem Run In Background: o player para ao perder o foco, então perder o foco também é segundo plano.</summary>
        DesktopStopsOnFocusLoss,

        /// <summary>Desktop com Run In Background: minimizar não para o jogo; nenhum aviso é segundo plano.</summary>
        DesktopKeepsRunning,
    }
}
