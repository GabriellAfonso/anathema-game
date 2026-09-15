#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Monta a dica de mira de uma cópia da mão. Nunca lança por estado de partida, e nenhum comando a consulta
    /// (specs/004-match-session/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// HandCardHint hint = HandCardHints.For(card.Instance, mirror.Current!, catalog, log);
    /// </code>
    /// </example>
    internal static class HandCardHints
    {
        /// <summary>A dica da cópia na visão dada; carta fora do catálogo é registrada no log, se houver um.</summary>
        /// <example><code>HandCardHint hint = HandCardHints.For(new CardInstanceId(24), view, catalog);</code></example>
        public static HandCardHint For(CardInstanceId card, PlayerView view, LoadedCatalog catalog, IClientLog? log = null)
        {
            if (view == null || catalog == null)
                throw new ArgumentNullException(view == null ? nameof(view) : nameof(catalog), $"hint for {card} needs the current view and the loaded catalog: expected both");

            MatchCard? inHand = view.You.Hand.FirstOrDefault(handCard => handCard.Instance == card);
            return inHand == null ? HandCardHint.NotInHand(card, view) : Describe(inHand, view, catalog, log);
        }

        private static HandCardHint Describe(MatchCard card, PlayerView view, LoadedCatalog catalog, IClientLog? log)
        {
            CardLookup lookup = catalog.Find(card.Card);
            if (lookup.Unit != null)
                return HandCardHint.ForUnit(card, lookup.Unit, view);

            if (lookup.Spell != null)
                return HandCardHint.ForSpell(card, lookup.Spell, view, Candidates(lookup.Spell.Effect.TargetKind, view));

            log?.Warning("hint_card_unknown", new LogField("card_id", card.Card.Value));
            return HandCardHint.UnknownCard(card, view);
        }

        private static IReadOnlyList<CardInstanceId> Candidates(SpellTargetKind target, PlayerView view)
        {
            return target switch
            {
                SpellTargetKind.AlliedUnit => view.You.Bank.Select(unit => unit.Card.Instance).ToArray(),
                SpellTargetKind.EnemyUnit => view.Opponent.Bank.Select(unit => unit.Card.Instance).ToArray(),
                _ => Array.Empty<CardInstanceId>(),
            };
        }
    }
}
