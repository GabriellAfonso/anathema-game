#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// A partida pelos olhos de um jogador, numa versão (<c>backend/server/apps/game/match/player_view.py</c> e
    /// <c>backend/specs/009-match-protocol/contracts/server_frames.md</c>). Imutável: o espelho troca a
    /// visão inteira a cada frame aceito, nunca mescla.
    /// </summary>
    /// <example>
    /// <code>
    /// PlayerView view = mirror.Current!;
    /// bool myTurn = view.PriorityUser == view.You.Profile.User;
    /// </code>
    /// </example>
    public sealed class PlayerView
    {
        private PlayerView(IPayloadReader view)
        {
            Match = view.ReadMatchId("match_id");
            RoundNumber = view.ReadInteger("round_number");
            PhaseText = view.ReadText("phase");
            Phase = MatchPhaseText.Parse(PhaseText);
            PriorityUser = view.ReadOptionalUserId("priority_user_id");
            TokenHolder = view.ReadOptionalUserId("token_holder_user_id");
            TokenConsumed = view.ReadBoolean("token_consumed");
            ConsecutivePasses = view.ReadInteger("consecutive_passes");
            Outcome = ReadOptional(view, "outcome", MatchOutcome.Read);
            Combat = ReadOptional(view, "combat", CombatView.Read);
            You = OwnSideView.Read(view.ReadObject("you"));
            Opponent = OpponentSideView.Read(view.ReadObject("opponent"));
        }

        /// <summary>A partida.</summary>
        /// <example><code>MatchId match = view.Match;</code></example>
        public MatchId Match { get; }

        /// <summary>Número da rodada.</summary>
        /// <example><code>long round = view.RoundNumber;</code></example>
        public long RoundNumber { get; }

        /// <summary>Fase.</summary>
        /// <example><code>bool declaring = view.Phase == MatchPhase.Declaration;</code></example>
        public MatchPhase Phase { get; }

        /// <summary>O texto de <c>phase</c> como veio.</summary>
        /// <example><code>string raw = view.PhaseText;</code></example>
        public string PhaseText { get; }

        /// <summary>De quem é a prioridade; nulo no mulligan.</summary>
        /// <example><code>UserId? priority = view.PriorityUser;</code></example>
        public UserId? PriorityUser { get; }

        /// <summary>Dono do token de ataque; nulo no mulligan.</summary>
        /// <example><code>UserId? holder = view.TokenHolder;</code></example>
        public UserId? TokenHolder { get; }

        /// <summary>O ataque da rodada já foi consumido.</summary>
        /// <example><code>bool consumed = view.TokenConsumed;</code></example>
        public bool TokenConsumed { get; }

        /// <summary>Passes seguidos.</summary>
        /// <example><code>long passes = view.ConsecutivePasses;</code></example>
        public long ConsecutivePasses { get; }

        /// <summary>Desfecho; nulo enquanto a partida corre.</summary>
        /// <example><code>MatchOutcome? outcome = view.Outcome;</code></example>
        public MatchOutcome? Outcome { get; }

        /// <summary>Combate em curso; nulo fora da declaração e do combate.</summary>
        /// <example><code>CombatView? combat = view.Combat;</code></example>
        public CombatView? Combat { get; }

        /// <summary>O próprio lado.</summary>
        /// <example><code>OwnSideView you = view.You;</code></example>
        public OwnSideView You { get; }

        /// <summary>O lado do oponente.</summary>
        /// <example><code>OpponentSideView opponent = view.Opponent;</code></example>
        public OpponentSideView Opponent { get; }

        /// <summary>Lê o objeto <c>view</c>.</summary>
        /// <example><code>PlayerView view = PlayerView.Read(payload.ReadObject("view"));</code></example>
        internal static PlayerView Read(IPayloadReader view) => new PlayerView(view);

        private static T? ReadOptional<T>(IPayloadReader view, string field, Func<IPayloadReader, T> read) where T : class
        {
            IPayloadReader? item = view.ReadOptionalObject(field);
            return item == null ? null : read(item);
        }
    }
}
