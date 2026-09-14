#nullable enable
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>Nome vazio, só espaços ou longo demais (chave <c>name</c> do 400).</summary>
    /// <example><code>nameHint.text = string.Join("\n", invalid.Messages);</code></example>
    public sealed class InvalidDeckName : DeckRefusalReason
    {
        /// <summary>Cria o motivo com as mensagens do servidor.</summary>
        /// <example><code>DeckRefusalReason reason = new InvalidDeckName(body.ReadTextList("name"));</code></example>
        public InvalidDeckName(IReadOnlyList<string> messages)
        {
            Messages = messages;
        }

        /// <summary>Mensagens do servidor.</summary>
        /// <example><code>string first = invalid.Messages[0];</code></example>
        public IReadOnlyList<string> Messages { get; }
    }
}
