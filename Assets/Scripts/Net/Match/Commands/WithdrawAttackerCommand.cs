#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>withdraw_attacker</c>: puxar um atacante de volta para o banco.</summary>
    /// <example><code>await commands.Send(new WithdrawAttackerCommand(attacker));</code></example>
    public sealed class WithdrawAttackerCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool withdraw = command.MessageType == WithdrawAttackerCommand.TypeName;</code></example>
        public const string TypeName = "withdraw_attacker";

        /// <summary>Puxar a cópia atacante.</summary>
        /// <example><code>PlayCommand withdraw = new WithdrawAttackerCommand(new CardInstanceId(22));</code></example>
        public WithdrawAttackerCommand(CardInstanceId attacker)
            : base(TypeName)
        {
            Attacker = attacker;
        }

        /// <summary>O atacante puxado.</summary>
        /// <example><code>CardInstanceId attacker = withdraw.Attacker;</code></example>
        public CardInstanceId Attacker { get; }

        /// <summary>Escreve <c>attacker_card_instance_id</c>.</summary>
        /// <example><code>withdraw.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
            writer.WriteCardInstanceId("attacker_card_instance_id", Attacker);
        }
    }
}
