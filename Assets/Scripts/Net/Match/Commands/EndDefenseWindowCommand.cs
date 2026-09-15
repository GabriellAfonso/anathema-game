#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>end_defense_window</c>: apertar Resolver na defesa.</summary>
    /// <example><code>await commands.Send(new EndDefenseWindowCommand());</code></example>
    public sealed class EndDefenseWindowCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool resolve = command.MessageType == EndDefenseWindowCommand.TypeName;</code></example>
        public const string TypeName = "end_defense_window";

        /// <summary>Fim da defesa, sem campos.</summary>
        /// <example><code>PlayCommand resolve = new EndDefenseWindowCommand();</code></example>
        public EndDefenseWindowCommand()
            : base(TypeName)
        {
        }

        /// <summary>Não escreve campo nenhum.</summary>
        /// <example><code>resolve.WritePayload(writer);</code></example>
        internal override void Write(IPayloadWriter writer)
        {
        }
    }
}
