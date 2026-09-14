#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>Em que ponto está a sessão de partida (specs/004-match-session/data-model.md, "Estados da sessão").</summary>
    /// <example>
    /// <code>
    /// reconnectBanner.SetActive(match.Status.Phase == LiveMatchPhase.Reconnecting);
    /// </code>
    /// </example>
    public enum LiveMatchPhase
    {
        /// <summary>Criada, ainda sem <c>Start</c>.</summary>
        Idle,

        /// <summary>Abrindo o socket, antes do primeiro <c>match_start</c>.</summary>
        Connecting,

        /// <summary>Estado espelhado em dia.</summary>
        Live,

        /// <summary>O socket caiu depois de ao vivo; o estado espelhado continua legível e marcado como desatualizado.</summary>
        Reconnecting,

        /// <summary>A partida acabou e o socket foi fechado de propósito.</summary>
        Finished,

        /// <summary>O servidor recusou a partida (<c>match_denied</c> ou 44xx).</summary>
        Refused,

        /// <summary>A conexão desistiu por outro motivo (sessão expirada, token recusado repetidamente, sem sessão).</summary>
        GaveUp,
    }
}
