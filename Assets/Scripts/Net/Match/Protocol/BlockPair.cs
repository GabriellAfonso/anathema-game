#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>Um par do bloqueio: quem bloqueia e quem é bloqueado (<c>documents.py</c>, <c>BlockAssignmentDocument</c>).</summary>
    /// <example>
    /// <code>
    /// foreach (BlockPair pair in view.Combat!.Blocks) DrawBlockLine(pair.Blocker, pair.Attacker);
    /// </code>
    /// </example>
    public sealed class BlockPair
    {
        private BlockPair(IPayloadReader pair)
        {
            Blocker = pair.ReadCardInstanceId("blocker_card_instance_id");
            Attacker = pair.ReadCardInstanceId("attacker_card_instance_id");
        }

        /// <summary>A unidade do defensor.</summary>
        /// <example><code>CardInstanceId blocker = pair.Blocker;</code></example>
        public CardInstanceId Blocker { get; }

        /// <summary>O atacante bloqueado.</summary>
        /// <example><code>CardInstanceId attacker = pair.Attacker;</code></example>
        public CardInstanceId Attacker { get; }

        /// <summary>Lê um objeto de <c>blocks</c>.</summary>
        /// <example><code>BlockPair pair = BlockPair.Read(item);</code></example>
        public static BlockPair Read(IPayloadReader pair) => new BlockPair(pair);
    }
}
