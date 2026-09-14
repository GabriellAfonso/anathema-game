#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>play_unit</c>: jogar uma unidade da mão.</summary>
    /// <example><code>await commands.Send(new PlayUnitCommand(card.Instance));</code></example>
    public sealed class PlayUnitCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool unit = command.MessageType == PlayUnitCommand.TypeName;</code></example>
        public const string TypeName = "play_unit";

        /// <summary>Jogada com a cópia da mão.</summary>
        /// <example><code>PlayCommand play = new PlayUnitCommand(new CardInstanceId(12));</code></example>
        public PlayUnitCommand(CardInstanceId card)
            : base(TypeName)
        {
            Card = card;
        }

        /// <summary>A cópia jogada.</summary>
        /// <example><code>CardInstanceId card = play.Card;</code></example>
        public CardInstanceId Card { get; }

        /// <summary>Escreve <c>card_instance_id</c>.</summary>
        /// <example><code>play.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
            writer.WriteCardInstanceId("card_instance_id", Card);
        }
    }
}
