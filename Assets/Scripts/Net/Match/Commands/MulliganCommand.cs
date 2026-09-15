#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary><c>mulligan</c>: as cópias da mão a trocar; lista vazia confirma sem trocar.</summary>
    /// <example><code>await commands.Send(new MulliganCommand(new[] { view.You.Hand[0].Instance }));</code></example>
    public sealed class MulliganCommand : PlayCommand
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>bool mulligan = command.MessageType == MulliganCommand.TypeName;</code></example>
        public const string TypeName = "mulligan";

        /// <summary>Mulligan com as cópias a trocar, na ordem dada.</summary>
        /// <example><code>PlayCommand keep = new MulliganCommand(Array.Empty&lt;CardInstanceId&gt;());</code></example>
        public MulliganCommand(IReadOnlyList<CardInstanceId> swapped)
            : base(TypeName)
        {
            Swapped = (swapped ?? throw new ArgumentNullException(nameof(swapped), "mulligan cards are null: expected a list of card_instance_id, possibly empty")).ToArray();
        }

        /// <summary>As cópias a trocar.</summary>
        /// <example><code>int count = mulligan.Swapped.Count;</code></example>
        public IReadOnlyList<CardInstanceId> Swapped { get; }

        /// <summary>Escreve <c>card_instance_ids</c>.</summary>
        /// <example><code>mulligan.WritePayload(writer);</code></example>
        internal override void Write(IPayloadWriter writer)
        {
            writer.WriteCardInstanceIdList("card_instance_ids", Swapped);
        }
    }
}
