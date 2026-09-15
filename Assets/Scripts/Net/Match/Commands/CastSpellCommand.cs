#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>cast_spell</c>: lançar um feitiço, com alvo ou sem. Sem alvo o campo <c>target_card_instance_id</c> é
    /// omitido, como no exemplo de <c>backend/specs/010-match-timers/contracts/client_messages.md</c>. Se o
    /// feitiço pede alvo quem decide é o servidor.
    /// </summary>
    /// <example><code>await commands.Send(new CastSpellCommand(spell.Instance, target));</code></example>
    public sealed class CastSpellCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool spell = command.MessageType == CastSpellCommand.TypeName;</code></example>
        public const string TypeName = "cast_spell";

        /// <summary>Feitiço com a cópia da mão e o alvo, ou nulo sem alvo.</summary>
        /// <example><code>PlayCommand fire = new CastSpellCommand(new CardInstanceId(23), null);</code></example>
        public CastSpellCommand(CardInstanceId card, CardInstanceId? target)
            : base(TypeName)
        {
            Card = card;
            Target = target;
        }

        /// <summary>A cópia do feitiço.</summary>
        /// <example><code>CardInstanceId card = cast.Card;</code></example>
        public CardInstanceId Card { get; }

        /// <summary>O alvo; nulo em feitiço sem alvo.</summary>
        /// <example><code>CardInstanceId? target = cast.Target;</code></example>
        public CardInstanceId? Target { get; }

        /// <summary>Escreve <c>card_instance_id</c> e, com alvo, <c>target_card_instance_id</c>.</summary>
        /// <example><code>cast.WritePayload(writer);</code></example>
        internal override void Write(IPayloadWriter writer)
        {
            writer.WriteCardInstanceId("card_instance_id", Card);
            if (Target.HasValue)
                writer.WriteCardInstanceId("target_card_instance_id", Target.Value);
        }
    }
}
