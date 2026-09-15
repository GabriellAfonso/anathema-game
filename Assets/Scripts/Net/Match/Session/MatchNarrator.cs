#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Escreve a partida no log: uma entrada <c>match_event</c> por evento aceito, com apelido no lugar do
    /// <c>user_id</c> e nome do catálogo no lugar do <c>card_id</c> (specs/004-match-session/contracts/live-match.md).
    /// Os campos vêm dos detalhes do próprio evento, sem <c>switch</c> por tipo (research R10).
    /// </summary>
    internal sealed class MatchNarrator : IDisposable
    {
        private readonly MatchMirror mirror;
        private readonly LoadedCatalog catalog;
        private readonly MatchId match;
        private readonly IClientLog log;
        private readonly IDisposable subscription;

        internal MatchNarrator(MatchMirror mirror, LoadedCatalog catalog, MatchId match, IClientLog log)
        {
            this.mirror = mirror;
            this.catalog = catalog;
            this.match = match;
            this.log = log;
            subscription = mirror.EventReceived.Subscribe(Narrate);
        }

        public void Dispose()
        {
            subscription.Dispose();
        }

        private void Narrate(MatchEvent item)
        {
            PlayerView? view = mirror.Current;
            List<LogField> fields = new List<LogField>
            {
                new LogField("match_id", match.Value),
                new LogField("round", view?.RoundNumber ?? 0),
                new LogField("kind", item.KindText),
            };
            if (item is UnrecognizedMatchEvent)
                fields.Add(new LogField("unrecognized", true));

            fields.AddRange(item.Details.Select(detail => new LogField(detail.Field, Describe(detail, view))));
            log.Info("match_event", fields.ToArray());
        }

        private string Describe(EventDetail detail, PlayerView? view)
        {
            if (detail.User.HasValue)
                return Nickname(detail.User.Value, view);
            if (detail.Card != null)
                return CardText(detail.Card);
            if (detail.Instance.HasValue)
                return Number(detail.Instance.Value.Value);
            if (detail.Instances != null)
                return string.Join(",", detail.Instances.Select(instance => Number(instance.Value)));
            if (detail.Cards != null)
                return string.Join(",", detail.Cards.Select(CardText));

            return detail.Number.HasValue ? Number(detail.Number.Value) : detail.Text ?? string.Empty;
        }

        private static string Nickname(UserId user, PlayerView? view)
        {
            if (view != null && view.You.Profile.User == user)
                return view.You.Profile.Nickname;

            return view != null && view.Opponent.Profile.User == user ? view.Opponent.Profile.Nickname : user.ToString();
        }

        private string CardText(MatchCard card)
        {
            CardLookup lookup = catalog.Find(card.Card);
            string? name = lookup.Unit?.Name ?? lookup.Spell?.Name;
            return name == null ? card.ToString() : name + "#" + Number(card.Instance.Value);
        }

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
