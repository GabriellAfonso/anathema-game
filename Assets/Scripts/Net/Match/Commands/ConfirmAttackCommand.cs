#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>confirm_attack</c>: apertar Atacar.</summary>
    /// <example><code>await commands.Send(new ConfirmAttackCommand());</code></example>
    public sealed class ConfirmAttackCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool confirm = command.MessageType == ConfirmAttackCommand.TypeName;</code></example>
        public const string TypeName = "confirm_attack";

        /// <summary>Confirmação, sem campos.</summary>
        /// <example><code>PlayCommand confirm = new ConfirmAttackCommand();</code></example>
        public ConfirmAttackCommand()
            : base(TypeName)
        {
        }

        /// <summary>Não escreve campo nenhum.</summary>
        /// <example><code>confirm.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
        }
    }
}
