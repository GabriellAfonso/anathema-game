#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>assign_blocker</c>: pôr uma unidade do banco na frente de um atacante.</summary>
    /// <example><code>await commands.Send(new AssignBlockerCommand(blocker: mine, attacker: theirs));</code></example>
    public sealed class AssignBlockerCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool block = command.MessageType == AssignBlockerCommand.TypeName;</code></example>
        public const string TypeName = "assign_blocker";

        /// <summary>Bloqueio com a cópia que bloqueia e o atacante.</summary>
        /// <example><code>PlayCommand block = new AssignBlockerCommand(new CardInstanceId(4), new CardInstanceId(21));</code></example>
        public AssignBlockerCommand(CardInstanceId blocker, CardInstanceId attacker)
            : base(TypeName)
        {
            Blocker = blocker;
            Attacker = attacker;
        }

        /// <summary>A unidade que bloqueia.</summary>
        /// <example><code>CardInstanceId blocker = block.Blocker;</code></example>
        public CardInstanceId Blocker { get; }

        /// <summary>O atacante bloqueado.</summary>
        /// <example><code>CardInstanceId attacker = block.Attacker;</code></example>
        public CardInstanceId Attacker { get; }

        /// <summary>Escreve <c>blocker_card_instance_id</c> e <c>attacker_card_instance_id</c>.</summary>
        /// <example><code>block.WritePayload(writer);</code></example>
        internal override void Write(IPayloadWriter writer)
        {
            writer.WriteCardInstanceId("blocker_card_instance_id", Blocker);
            writer.WriteCardInstanceId("attacker_card_instance_id", Attacker);
        }
    }
}
