#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Por que a busca acabou sem pareamento e sem o jogador pedir.</summary>
    /// <example><code>if (exit.Kind == QueueExitKind.ConnectionGaveUp) ShowReason(exit.GiveUp!.PlayerText());</code></example>
    public enum QueueExitKind
    {
        /// <summary>A conexão de fila desistiu por um motivo que não é de sessão.</summary>
        ConnectionGaveUp,

        /// <summary>O servidor pareou mas não achou o perfil de um dos jogadores (<c>matchmaking_failed</c>).</summary>
        MatchmakingFailed,
    }
}
