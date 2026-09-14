#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Qual gate da partida recusou, lido do close code (FR-007).</summary>
    /// <example><code>if (reason.Match == MatchRefusalDetail.MatchNotFound) BackToHome();</code></example>
    public enum MatchRefusalDetail
    {
        /// <summary>Socket aberto sem matchId (4400).</summary>
        NoMatchId,

        /// <summary>O jogador não joga a partida (4403).</summary>
        NotAParticipant,

        /// <summary>A partida não existe (4404).</summary>
        MatchNotFound,

        /// <summary><c>match_denied</c> chegou, mas o fechamento não trouxe código de gate.</summary>
        Unspecified,
    }
}
