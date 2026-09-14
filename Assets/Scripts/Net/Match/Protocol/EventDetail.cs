#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Um campo de evento com o nome do contrato e um valor tipado, só um preenchido. Serve para descrever o
    /// evento sem um <c>switch</c> por tipo: o narrador resolve apelido e nome de carta
    /// (specs/004-match-session/research.md, R10).
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (EventDetail detail in matchEvent.Details) Write(detail.Field, detail.User?.ToString() ?? detail.Text);
    /// </code>
    /// </example>
    public sealed class EventDetail
    {
        private EventDetail(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException($"event detail field is '{field}': expected the contract field name, like user_id", nameof(field));

            Field = field;
        }

        /// <summary>Nome do campo no contrato.</summary>
        /// <example><code>string name = detail.Field; // "user_id"</code></example>
        public string Field { get; }

        /// <summary>Jogador, quando o campo é um <c>*_user_id</c>.</summary>
        /// <example><code>UserId? user = detail.User;</code></example>
        public UserId? User { get; private set; }

        /// <summary>Carta, quando o campo é um <c>card</c>.</summary>
        /// <example><code>MatchCard? card = detail.Card;</code></example>
        public MatchCard? Card { get; private set; }

        /// <summary>Cópia, quando o campo é um <c>*_card_instance_id</c>.</summary>
        /// <example><code>CardInstanceId? instance = detail.Instance;</code></example>
        public CardInstanceId? Instance { get; private set; }

        /// <summary>Lista de cópias.</summary>
        /// <example><code>int count = detail.Instances?.Count ?? 0;</code></example>
        public IReadOnlyList<CardInstanceId>? Instances { get; private set; }

        /// <summary>Lista de cartas.</summary>
        /// <example><code>int count = detail.Cards?.Count ?? 0;</code></example>
        public IReadOnlyList<MatchCard>? Cards { get; private set; }

        /// <summary>Número.</summary>
        /// <example><code>long? amount = detail.Number;</code></example>
        public long? Number { get; private set; }

        /// <summary>Texto.</summary>
        /// <example><code>string? reason = detail.Text;</code></example>
        public string? Text { get; private set; }

        /// <summary>Campo de jogador.</summary>
        /// <example><code>EventDetail.OfUser("user_id", user);</code></example>
        public static EventDetail OfUser(string field, UserId user) => new EventDetail(field) { User = user };

        /// <summary>Campo de carta.</summary>
        /// <example><code>EventDetail.OfCard("card", card);</code></example>
        public static EventDetail OfCard(string field, MatchCard card) => new EventDetail(field) { Card = card ?? throw NullValue(field) };

        /// <summary>Campo de cópia.</summary>
        /// <example><code>EventDetail.OfInstance("blocker_card_instance_id", blocker);</code></example>
        public static EventDetail OfInstance(string field, CardInstanceId instance) => new EventDetail(field) { Instance = instance };

        /// <summary>Campo de lista de cópias.</summary>
        /// <example><code>EventDetail.OfInstances("attacker_card_instance_ids", attackers);</code></example>
        public static EventDetail OfInstances(string field, IReadOnlyList<CardInstanceId> instances) => new EventDetail(field) { Instances = instances ?? throw NullValue(field) };

        /// <summary>Campo de lista de cartas.</summary>
        /// <example><code>EventDetail.OfCards("cards", cards);</code></example>
        public static EventDetail OfCards(string field, IReadOnlyList<MatchCard> cards) => new EventDetail(field) { Cards = cards ?? throw NullValue(field) };

        /// <summary>Campo numérico.</summary>
        /// <example><code>EventDetail.OfNumber("amount", 3);</code></example>
        public static EventDetail OfNumber(string field, long number) => new EventDetail(field) { Number = number };

        /// <summary>Campo de texto.</summary>
        /// <example><code>EventDetail.OfText("reason", "forfeit");</code></example>
        public static EventDetail OfText(string field, string text) => new EventDetail(field) { Text = text ?? throw NullValue(field) };

        private static ArgumentNullException NullValue(string field)
        {
            return new ArgumentNullException(field, $"event detail '{field}' value is null: expected the value read from the event");
        }
    }
}
