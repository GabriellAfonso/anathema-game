#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Junta segundo plano e falta de rede em dois flags e avisa quando a conexão deve parar de tentar e
    /// quando deve voltar. Quem decide o que fazer com o socket é a conexão
    /// (specs/003-authenticated-socket-queue/research.md, R9).
    /// </summary>
    internal sealed class ConnectionSuspension : IDisposable
    {
        private readonly IAppLifecycle lifecycle;
        private readonly INetworkReachability reachability;

        internal ConnectionSuspension(IAppLifecycle lifecycle, INetworkReachability reachability)
        {
            this.lifecycle = lifecycle;
            this.reachability = reachability;
            WithoutNetwork = reachability.Current == NetworkKind.None;
            lifecycle.WentToBackground += OnBackground;
            lifecycle.ReturnedToForeground += OnForeground;
            reachability.Changed += OnNetworkChanged;
        }

        /// <summary>Parou de tentar: foi para segundo plano ou ficou sem rede.</summary>
        internal event Action? Suspended;

        /// <summary>Voltou ao primeiro plano, ou a rede voltou ou trocou de tipo.</summary>
        internal event Action<ResumeCause>? Resumed;

        internal bool InBackground { get; private set; }

        internal bool WithoutNetwork { get; private set; }

        internal bool IsSuspended => InBackground || WithoutNetwork;

        /// <summary>Motivo da suspensão atual; segundo plano ganha de sem rede.</summary>
        internal SuspensionReason? Reason => InBackground ? SuspensionReason.Background : WithoutNetwork ? SuspensionReason.NoNetwork : (SuspensionReason?)null;

        /// <summary>A última mudança de rede vista, para o log da reciclagem.</summary>
        internal NetworkKindChanged? LastChange { get; private set; }

        public void Dispose()
        {
            lifecycle.WentToBackground -= OnBackground;
            lifecycle.ReturnedToForeground -= OnForeground;
            reachability.Changed -= OnNetworkChanged;
        }

        private void OnBackground(WentToBackground signal)
        {
            InBackground = true;
            Suspended?.Invoke();
        }

        private void OnForeground(ReturnedToForeground signal)
        {
            InBackground = false;
            Resumed?.Invoke(ResumeCause.Foreground);
        }

        private void OnNetworkChanged(NetworkKindChanged change)
        {
            LastChange = change;
            if (change.Current == NetworkKind.None)
            {
                WithoutNetwork = true;
                Suspended?.Invoke();
                return;
            }

            WithoutNetwork = false;
            Resumed?.Invoke(change.Previous == NetworkKind.None ? ResumeCause.NetworkBack : ResumeCause.NetworkKindChanged);
        }
    }
}
