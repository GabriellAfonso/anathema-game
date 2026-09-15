#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>O que o cliente deve fazer depois que o socket fechou.</summary>
    /// <example><code>if (plan.Action == ReconnectAction.GiveUp) GiveUp(plan.Reason);</code></example>
    internal enum ReconnectAction
    {
        /// <summary>Espera o delay e conecta de novo com o token atual.</summary>
        Retry,

        /// <summary>Renova o access token antes de tentar: o servidor recusou este.</summary>
        RefreshTokenThenRetry,

        /// <summary>Reconectar nao vai adiantar. Avisa o jogador.</summary>
        GiveUp,
    }
}
