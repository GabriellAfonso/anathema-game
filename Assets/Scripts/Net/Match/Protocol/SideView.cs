#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O que os dois lados da visão têm em comum (<c>backend/server/apps/game/match/player_view.py</c>). A mão
    /// só existe no próprio lado (<see cref="OwnSideView"/>); do oponente sai só o tamanho.
    /// </summary>
    /// <example>
    /// <code>
    /// void DrawNexus(SideView side) => nexusLabel.text = side.Nexus.ToString();
    /// </code>
    /// </example>
    public abstract class SideView
    {
        private protected SideView(IPayloadReader side)
        {
            Profile = MatchProfile.Read(side.ReadObject("profile"));
            Nexus = side.ReadInteger("nexus");
            EnergyCurrent = side.ReadInteger("energy_current");
            Bank = BankUnit.ReadList(side, "bank");
            Graveyard = MatchCard.ReadList(side, "graveyard");
            DeckSize = side.ReadInteger("deck_size");
            MulliganTaken = side.ReadBoolean("mulligan_taken");
        }

        /// <summary>Perfil do jogador deste lado.</summary>
        /// <example><code>UserId user = side.Profile.User;</code></example>
        public MatchProfile Profile { get; }

        /// <summary>Nexus.</summary>
        /// <example><code>long nexus = side.Nexus;</code></example>
        public long Nexus { get; }

        /// <summary>Energia atual.</summary>
        /// <example><code>long energy = side.EnergyCurrent;</code></example>
        public long EnergyCurrent { get; }

        /// <summary>Unidades no banco, na ordem do servidor.</summary>
        /// <example><code>int units = side.Bank.Count;</code></example>
        public IReadOnlyList<BankUnit> Bank { get; }

        /// <summary>Cemitério, informação já revelada.</summary>
        /// <example><code>int dead = side.Graveyard.Count;</code></example>
        public IReadOnlyList<MatchCard> Graveyard { get; }

        /// <summary>Cartas no deck; o conteúdo nunca sai do servidor.</summary>
        /// <example><code>long left = side.DeckSize;</code></example>
        public long DeckSize { get; }

        /// <summary>Este lado já respondeu o mulligan.</summary>
        /// <example><code>bool answered = side.MulliganTaken;</code></example>
        public bool MulliganTaken { get; }
    }
}
