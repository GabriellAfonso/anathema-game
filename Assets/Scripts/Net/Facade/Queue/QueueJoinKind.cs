#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Como terminou um pedido de entrar na fila.</summary>
    /// <example><code>if (result.Kind == QueueJoinKind.NotApplicable) log.Warning("join_ignored");</code></example>
    public enum QueueJoinKind
    {
        /// <summary>Entrou: o app foi para <see cref="ClientStage.Searching"/>.</summary>
        Started,

        /// <summary>Já estava procurando; nada foi enviado.</summary>
        AlreadyQueued,

        /// <summary>O estágio não permite entrar na fila; nada foi enviado.</summary>
        NotApplicable,
    }
}
