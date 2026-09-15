#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Uma carta em partida: a cópia (<see cref="Instance"/>, citada nas jogadas) e a entrada do catálogo
    /// (<see cref="Card"/>, só para buscar nome, custo, ataque e vida)
    /// (<c>backend/server/apps/game/match/documents.py</c>, <c>CardDocument</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// commands.PlayUnit(handCard.Instance);
    /// string name = catalog.Find(handCard.Card).Unit?.Name ?? "?";
    /// </code>
    /// </example>
    public sealed class MatchCard : IEquatable<MatchCard>
    {
        /// <summary>Carta com a cópia e a entrada do catálogo.</summary>
        /// <example><code>MatchCard card = new MatchCard(new CardInstanceId(21), new CardId(5));</code></example>
        public MatchCard(CardInstanceId instance, CardId card)
        {
            Instance = instance;
            Card = card;
        }

        /// <summary>A cópia nesta partida.</summary>
        /// <example><code>CardInstanceId chosen = card.Instance;</code></example>
        public CardInstanceId Instance { get; }

        /// <summary>A entrada do catálogo.</summary>
        /// <example><code>CardLookup lookup = catalog.Find(card.Card);</code></example>
        public CardId Card { get; }

        /// <summary>Lê um objeto <c>{card_instance_id, card_id}</c>.</summary>
        /// <example><code>MatchCard card = MatchCard.Read(item);</code></example>
        internal static MatchCard Read(IPayloadReader card)
        {
            return new MatchCard(card.ReadCardInstanceId("card_instance_id"), card.ReadCardId("card_id"));
        }

        /// <summary>Lê uma lista de cartas, na ordem.</summary>
        /// <example><code>IReadOnlyList&lt;MatchCard&gt; hand = MatchCard.ReadList(side, "hand");</code></example>
        internal static IReadOnlyList<MatchCard> ReadList(IPayloadReader parent, string field)
        {
            return parent.ReadObjectList(field).Select(Read).ToArray();
        }

        /// <summary>Mesma cópia e mesma entrada.</summary>
        /// <example><code>bool same = card.Equals(other);</code></example>
        public bool Equals(MatchCard? other) => other != null && Instance == other.Instance && Card == other.Card;

        /// <summary>Igualdade por valor.</summary>
        /// <example><code>bool same = card.Equals((object)other);</code></example>
        public override bool Equals(object? obj) => Equals(obj as MatchCard);

        /// <summary>Hash dos dois identificadores.</summary>
        /// <example><code>int hash = card.GetHashCode();</code></example>
        public override int GetHashCode() => (Instance.GetHashCode() * 397) ^ Card.GetHashCode();

        /// <summary>Os dois identificadores, para log.</summary>
        /// <example><code>string text = card.ToString(); // card_instance_id=21 card_id=5</code></example>
        public override string ToString() => $"{Instance} {Card}";
    }
}
