#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>remove_blocker</c>: tirar um bloqueador já atribuído.</summary>
    /// <example><code>await commands.Send(new RemoveBlockerCommand(blocker));</code></example>
    public sealed class RemoveBlockerCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool remove = command.MessageType == RemoveBlockerCommand.TypeName;</code></example>
        public const string TypeName = "remove_blocker";

        /// <summary>Remoção da cópia que bloqueia.</summary>
        /// <example><code>PlayCommand remove = new RemoveBlockerCommand(new CardInstanceId(4));</code></example>
        public RemoveBlockerCommand(CardInstanceId blocker)
            : base(TypeName)
        {
            Blocker = blocker;
        }

        /// <summary>O bloqueador tirado.</summary>
        /// <example><code>CardInstanceId blocker = remove.Blocker;</code></example>
        public CardInstanceId Blocker { get; }

        /// <summary>Escreve <c>blocker_card_instance_id</c>.</summary>
        /// <example><code>remove.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
            writer.WriteCardInstanceId("blocker_card_instance_id", Blocker);
        }
    }
}
