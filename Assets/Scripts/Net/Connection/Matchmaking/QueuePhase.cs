#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Onde o jogador está em relação à fila (spec, US2).</summary>
    /// <example><code>playButton.interactable = queue.Phase == QueuePhase.OutOfQueue;</code></example>
    public enum QueuePhase
    {
        /// <summary>Fora da fila.</summary>
        OutOfQueue,

        /// <summary>Abrindo ou reabrindo o socket para mandar o <c>join_queue</c>.</summary>
        Connecting,

        /// <summary><c>join_queue</c> mandado e sem recusa.</summary>
        Searching,

        /// <summary><c>match_found</c> recebido; o socket de fila foi fechado.</summary>
        Paired,
    }
}
