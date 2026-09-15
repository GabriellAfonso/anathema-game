#nullable enable
using System;

namespace Anathema.Net.Facade
{
    /// <summary>Uma transição do estado do app: o anterior e o novo, com o que a tela seguinte precisa.</summary>
    /// <example>
    /// <code>
    /// subscriptions.Add(client.StageChanged.Subscribe(change =>
    /// {
    ///     if (change.Current.Stage == ClientStage.Paired) ShowVersus(change.Current.Pairing!);
    /// }));
    /// </code>
    /// </example>
    public sealed class ClientStageChange
    {
        internal ClientStageChange(ClientState previous, ClientState current)
        {
            Previous = previous ?? throw new ArgumentNullException(nameof(previous), "previous state is null: expected the state before the transition");
            Current = current ?? throw new ArgumentNullException(nameof(current), "current state is null: expected the state after the transition");
        }

        /// <summary>O estado antes da transição.</summary>
        /// <example><code>bool fromMatch = change.Previous.Stage == ClientStage.InMatch;</code></example>
        public ClientState Previous { get; }

        /// <summary>O estado depois da transição.</summary>
        /// <example><code>ClientStage now = change.Current.Stage;</code></example>
        public ClientState Current { get; }
    }
}
