#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Um aviso por quadro, na thread principal, depois de a <see cref="MainThreadQueue"/> drenar. É
    /// o passo da camada de rede: esperas e silêncio são avaliados a cada aviso contra o
    /// <see cref="IMonotonicClock"/>, sem timer nem Task.Delay
    /// (specs/003-authenticated-socket-queue/research.md, R2).
    /// </summary>
    /// <example>
    /// <code>
    /// ticker.Ticked += () => EvaluateRetry(clock.Now);
    /// </code>
    /// </example>
    internal interface IFrameTicker
    {
        /// <summary>Passou um quadro.</summary>
        event Action? Ticked;
    }
}
