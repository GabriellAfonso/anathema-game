#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Evento com <c>kind</c> que o cliente não conhece. Não é erro: o servidor pode contar algo novo antes de o
    /// cliente saber narrar; o estado continua vindo inteiro na visão.
    /// </summary>
    /// <example>
    /// <code>
    /// if (matchEvent is UnrecognizedMatchEvent unknown) log.Debug("event_unknown", new LogField("kind", unknown.KindText));
    /// </code>
    /// </example>
    public sealed class UnrecognizedMatchEvent : MatchEvent
    {
        /// <summary>Evento desconhecido com o texto exato de <c>kind</c>.</summary>
        /// <example><code>MatchEvent unknown = new UnrecognizedMatchEvent("card_discarded");</code></example>
        public UnrecognizedMatchEvent(string kindText)
            : base(kindText)
        {
        }

        /// <summary>Sem campos: o cliente não sabe quais são.</summary>
        /// <example><code>int none = unknown.Details.Count; // 0</code></example>
        public override IReadOnlyList<EventDetail> Details => Array.Empty<EventDetail>();
    }
}
