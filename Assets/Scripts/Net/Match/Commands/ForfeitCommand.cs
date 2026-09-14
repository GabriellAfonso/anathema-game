#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>forfeit</c>: desistir. A partida só termina quando o servidor manda a fase <c>finished</c>.</summary>
    /// <example><code>await commands.Send(new ForfeitCommand());</code></example>
    public sealed class ForfeitCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool forfeit = command.MessageType == ForfeitCommand.TypeName;</code></example>
        public const string TypeName = "forfeit";

        /// <summary>Desistência, sem campos.</summary>
        /// <example><code>PlayCommand forfeit = new ForfeitCommand();</code></example>
        public ForfeitCommand()
            : base(TypeName)
        {
        }

        /// <summary>Não escreve campo nenhum.</summary>
        /// <example><code>forfeit.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
        }
    }
}
