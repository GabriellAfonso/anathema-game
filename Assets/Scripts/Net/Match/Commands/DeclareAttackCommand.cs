#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>declare_attack</c>: mandar unidades para a zona de ataque. Lista vazia é enviada; quem recusa é o servidor.</summary>
    /// <example><code>await commands.Send(new DeclareAttackCommand(view.You.Bank.Select(unit => unit.Card.Instance).ToArray()));</code></example>
    public sealed class DeclareAttackCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool attack = command.MessageType == DeclareAttackCommand.TypeName;</code></example>
        public const string TypeName = "declare_attack";

        /// <summary>Declaração com as cópias atacantes, na ordem dada.</summary>
        /// <example><code>PlayCommand attack = new DeclareAttackCommand(new[] { new CardInstanceId(21) });</code></example>
        public DeclareAttackCommand(IReadOnlyList<CardInstanceId> attackers)
            : base(TypeName)
        {
            Attackers = (attackers ?? throw new ArgumentNullException(nameof(attackers), "attackers are null: expected a list of card_instance_id, possibly empty")).ToArray();
        }

        /// <summary>As cópias mandadas.</summary>
        /// <example><code>int count = attack.Attackers.Count;</code></example>
        public IReadOnlyList<CardInstanceId> Attackers { get; }

        /// <summary>Escreve <c>attacker_card_instance_ids</c>.</summary>
        /// <example><code>attack.WritePayload(writer);</code></example>
        public override void WritePayload(IPayloadWriter writer)
        {
            writer.WriteCardInstanceIdList("attacker_card_instance_ids", Attackers);
        }
    }
}
