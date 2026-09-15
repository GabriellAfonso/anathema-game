#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O combate em curso, visível aos dois lados (<c>documents.py</c>, <c>CombatDocument</c>). Sem combate a
    /// visão traz <c>null</c>; <see cref="Blocks"/> vazia é combate sem bloqueador.
    /// </summary>
    /// <example>
    /// <code>
    /// if (view.Combat != null) DrawAttackZone(view.Combat.Attackers);
    /// </code>
    /// </example>
    public sealed class CombatView
    {
        private CombatView(IPayloadReader combat)
        {
            Attackers = combat.ReadCardInstanceIdList("attacker_card_instance_ids");
            Blocks = combat.ReadObjectList("blocks").Select(BlockPair.Read).ToArray();
        }

        /// <summary>Unidades na zona de ataque, na ordem.</summary>
        /// <example><code>CardInstanceId first = combat.Attackers[0];</code></example>
        public IReadOnlyList<CardInstanceId> Attackers { get; }

        /// <summary>Pares de bloqueio atribuídos.</summary>
        /// <example><code>bool anyBlock = combat.Blocks.Count > 0;</code></example>
        public IReadOnlyList<BlockPair> Blocks { get; }

        /// <summary>Lê o objeto <c>combat</c>.</summary>
        /// <example><code>CombatView combat = CombatView.Read(view.ReadObject("combat"));</code></example>
        internal static CombatView Read(IPayloadReader combat) => new CombatView(combat);
    }
}
