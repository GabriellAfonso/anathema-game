#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Ida e volta do segundo plano. No máximo um "foi" por "voltou", e a
    /// sequência nunca começa com "voltou" (FR-017).
    /// </summary>
    /// <example>
    /// <code>
    /// lifecycle.WentToBackground += _ => socket.Close();
    /// lifecycle.ReturnedToForeground += signal => Reconnect(signal.AwayFor);
    /// </code>
    /// </example>
    public interface IAppLifecycle
    {
        /// <summary>O app saiu do primeiro plano.</summary>
        event Action<WentToBackground>? WentToBackground;

        /// <summary>O app voltou, com a duração fora.</summary>
        event Action<ReturnedToForeground>? ReturnedToForeground;
    }
}
