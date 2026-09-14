#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>pass</c>: passar a vez.</summary>
    /// <example><code>await commands.Send(new PassCommand());</code></example>
    public sealed class PassCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool pass = command.MessageType == PassCommand.TypeName;</code></example>
        public const string TypeName = "pass";

        /// <summary>Passe, sem campos.</summary>
        /// <example><code>PlayCommand pass = new PassCommand();</code></example>
        public PassCommand()
            : base(TypeName)
        {
        }

        /// <summary>Não escreve campo nenhum.</summary>
        /// <example><code>pass.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
        }
    }
}
