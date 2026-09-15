#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>O que o client deve fazer neste frame.</summary>
    /// <example><code>if (heartbeat.Tick(delta) == HeartbeatAction.DeclareDead) Drop();</code></example>
    internal enum HeartbeatAction
    {
        /// <summary>Nada a fazer.</summary>
        Idle,

        /// <summary>Hora de mandar um ping.</summary>
        SendPing,

        /// <summary>Silencio longo demais: trate a conexao como morta.</summary>
        DeclareDead,
    }
}
