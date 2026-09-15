#nullable enable
using System.Collections.Generic;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Um evento de <c>match_update</c>, união por <c>kind</c>: primeiro a jogada, depois as consequências
    /// (<c>backend/specs/009-match-protocol/data-model.md</c>, "Eventos", e
    /// <c>backend/specs/010-match-timers/contracts/server_frames.md</c>). Eventos só contam o que aconteceu; o
    /// estado a desenhar é a visão.
    /// </summary>
    /// <example>
    /// <code>
    /// mirror.EventReceived.Subscribe(matchEvent => { if (matchEvent is UnitDiedEvent died) PlayDeath(died.Card); });
    /// </code>
    /// </example>
    public abstract class MatchEvent
    {
        private protected MatchEvent(string kindText)
        {
            KindText = kindText;
        }

        /// <summary>O texto de <c>kind</c> como veio.</summary>
        /// <example><code>string kind = matchEvent.KindText; // "unit_played"</code></example>
        public string KindText { get; }

        /// <summary>Os campos do evento, na ordem do contrato, com o nome de cada um.</summary>
        /// <example><code>int fields = matchEvent.Details.Count;</code></example>
        public abstract IReadOnlyList<EventDetail> Details { get; }
    }
}
