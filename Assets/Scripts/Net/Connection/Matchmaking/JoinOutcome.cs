#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>O que aconteceu com um pedido de entrar na fila (FR-027, FR-028).</summary>
    /// <example><code>if (queue.Join(deck) == JoinOutcome.AlreadyQueued) return;</code></example>
    public enum JoinOutcome
    {
        /// <summary>A busca começou.</summary>
        Started,

        /// <summary>Já conectando ou procurando; nada foi mandado. Trocar de deck é sair e entrar.</summary>
        AlreadyQueued,
    }
}
