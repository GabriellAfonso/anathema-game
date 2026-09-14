#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>O servidor pareou: a partida e os dois jogadores, do ponto de vista de quem recebe (FR-030).</summary>
    /// <example>
    /// <code>
    /// queue.Paired += pairing => match.Connect(pairing.Match);
    /// </code>
    /// </example>
    public sealed class MatchPairing
    {
        /// <summary>Pareamento lido do <c>match_found</c>.</summary>
        /// <example><code>MatchPairing pairing = new MatchPairing(new MatchId("m-1"), self, opponent);</code></example>
        public MatchPairing(MatchId match, PairedPlayer self, PairedPlayer opponent)
        {
            Match = match;
            Self = self ?? throw new ArgumentNullException(nameof(self), $"self of {match} is null: expected the player who received match_found");
            Opponent = opponent ?? throw new ArgumentNullException(nameof(opponent), $"opponent of {match} is null: expected the paired opponent");
        }

        /// <summary>A partida que o socket de partida abre em seguida.</summary>
        /// <example><code>ConnectionTarget target = ConnectionTarget.Match(routes.Match, pairing.Match);</code></example>
        public MatchId Match { get; }

        /// <summary>O próprio jogador.</summary>
        /// <example><code>string myName = pairing.Self.Nickname;</code></example>
        public PairedPlayer Self { get; }

        /// <summary>O oponente.</summary>
        /// <example><code>string theirName = pairing.Opponent.Nickname;</code></example>
        public PairedPlayer Opponent { get; }
    }
}
