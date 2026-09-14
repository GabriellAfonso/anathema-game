#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Recusa de uma operação de deck, com todos os motivos reconhecidos no mesmo corpo (FR-033).
    /// Nunca vazia.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; created = await decks.CreateAsync(draft);
    /// DeckListRejected? rejected = created.Refusal?.Reasons.OfType&lt;DeckListRejected&gt;().FirstOrDefault();
    /// </code>
    /// </example>
    public sealed class DeckRefusal
    {
        private DeckRefusal(IReadOnlyList<DeckRefusalReason> reasons)
        {
            Reasons = reasons;
        }

        /// <summary>Motivos, na ordem da tabela de recusas.</summary>
        /// <example><code>DeckRefusalReason first = refusal.Reasons[0];</code></example>
        public IReadOnlyList<DeckRefusalReason> Reasons { get; }

        /// <summary>Recusa com os motivos dados; nenhum motivo lança.</summary>
        /// <example><code>return DeckRefusal.Of(DeckNotFound.Instance);</code></example>
        public static DeckRefusal Of(params DeckRefusalReason[] reasons)
        {
            if (reasons == null || reasons.Length == 0)
                throw new ArgumentException($"deck refusal has {reasons?.Length ?? 0} reasons: expected at least one", nameof(reasons));

            return new DeckRefusal(reasons);
        }

        /// <summary>Forma para log, com os tipos dos motivos.</summary>
        /// <example><code>string text = refusal.ToString(); // InvalidDeckName, DeckListRejected</code></example>
        public override string ToString() => string.Join(", ", Reasons.Select(reason => reason.GetType().Name));
    }
}
