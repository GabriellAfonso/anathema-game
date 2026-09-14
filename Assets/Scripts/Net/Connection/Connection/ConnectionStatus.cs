#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// A fase da conexão com os dados que só aquela fase tem: tentativa e espera em
    /// <see cref="ConnectionPhase.WaitingRetry"/>, motivo em <see cref="ConnectionPhase.Suspended"/> e
    /// em <see cref="ConnectionPhase.GaveUp"/>. Igualdade por valor, para a conexão só avisar mudança.
    /// </summary>
    /// <example>
    /// <code>
    /// connection.StatusChanged += status => overlay.Show(status.Phase, status.Attempt);
    /// </code>
    /// </example>
    public sealed class ConnectionStatus : IEquatable<ConnectionStatus>
    {
        private ConnectionStatus(ConnectionPhase phase, int attempt, TimeSpan wait, SuspensionReason? suspension, GiveUpReason? giveUp)
        {
            Phase = phase;
            Attempt = attempt;
            Wait = wait;
            Suspension = suspension;
            GiveUp = giveUp;
        }

        /// <summary>A fase.</summary>
        /// <example><code>ConnectionPhase phase = status.Phase;</code></example>
        public ConnectionPhase Phase { get; }

        /// <summary>Número da tentativa; só em <see cref="ConnectionPhase.WaitingRetry"/>, zero nas outras.</summary>
        /// <example><code>int attempt = status.Attempt;</code></example>
        public int Attempt { get; }

        /// <summary>Espera até a tentativa; só em <see cref="ConnectionPhase.WaitingRetry"/>.</summary>
        /// <example><code>TimeSpan wait = status.Wait;</code></example>
        public TimeSpan Wait { get; }

        /// <summary>Motivo da suspensão; só em <see cref="ConnectionPhase.Suspended"/>.</summary>
        /// <example><code>SuspensionReason? reason = status.Suspension;</code></example>
        public SuspensionReason? Suspension { get; }

        /// <summary>Motivo da desistência; só em <see cref="ConnectionPhase.GaveUp"/>.</summary>
        /// <example><code>string text = status.GiveUp!.PlayerText();</code></example>
        public GiveUpReason? GiveUp { get; }

        /// <summary>Status de uma fase sem dados: desconectado, conectando, conectado ou renovando token.</summary>
        /// <example><code>ConnectionStatus connecting = ConnectionStatus.Of(ConnectionPhase.Connecting);</code></example>
        public static ConnectionStatus Of(ConnectionPhase phase)
        {
            if (phase == ConnectionPhase.WaitingRetry || phase == ConnectionPhase.Suspended || phase == ConnectionPhase.GaveUp)
                throw new ArgumentException($"phase is {phase}: expected Disconnected, Connecting, Connected or RenewingToken; use Waiting, SuspendedBy or GivenUp for phases with data", nameof(phase));

            return new ConnectionStatus(phase, 0, TimeSpan.Zero, null, null);
        }

        /// <summary>Esperando a tentativa <paramref name="attempt"/> daqui a <paramref name="wait"/>.</summary>
        /// <example><code>ConnectionStatus waiting = ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1));</code></example>
        public static ConnectionStatus Waiting(int attempt, TimeSpan wait)
        {
            if (attempt < 1)
                throw new ArgumentOutOfRangeException(nameof(attempt), attempt, $"retry attempt is {attempt}: expected 1 or more");

            if (wait < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(wait), wait, $"retry wait is {wait}: expected a non-negative duration");

            return new ConnectionStatus(ConnectionPhase.WaitingRetry, attempt, wait, null, null);
        }

        /// <summary>Suspensa pelo motivo dado.</summary>
        /// <example><code>ConnectionStatus suspended = ConnectionStatus.SuspendedBy(SuspensionReason.Background);</code></example>
        public static ConnectionStatus SuspendedBy(SuspensionReason reason)
        {
            return new ConnectionStatus(ConnectionPhase.Suspended, 0, TimeSpan.Zero, reason, null);
        }

        /// <summary>Desistiu pelo motivo dado.</summary>
        /// <example><code>ConnectionStatus gaveUp = ConnectionStatus.GivenUp(GiveUpReason.SessionExpired());</code></example>
        public static ConnectionStatus GivenUp(GiveUpReason reason)
        {
            GiveUpReason required = reason ?? throw new ArgumentNullException(nameof(reason), "give up reason is null: expected why the connection stopped trying");
            return new ConnectionStatus(ConnectionPhase.GaveUp, 0, TimeSpan.Zero, null, required);
        }

        /// <inheritdoc />
        public bool Equals(ConnectionStatus? other)
        {
            return other != null && Phase == other.Phase && Attempt == other.Attempt && Wait == other.Wait
                && Suspension == other.Suspension && Equals(GiveUp, other.GiveUp);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as ConnectionStatus);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Phase * 397) ^ Attempt ^ Wait.GetHashCode();

        /// <inheritdoc />
        public override string ToString()
        {
            return Phase switch
            {
                ConnectionPhase.WaitingRetry => $"{Phase} attempt={Attempt} wait_ms={(long)Wait.TotalMilliseconds}",
                ConnectionPhase.Suspended => $"{Phase} reason={Suspension}",
                ConnectionPhase.GaveUp => $"{Phase} {GiveUp}",
                _ => Phase.ToString(),
            };
        }
    }
}
