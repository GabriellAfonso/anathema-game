#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Uma unidade no banco: a carta, o dano acumulado e os modificadores
    /// (<c>backend/server/apps/game/match/documents.py</c>, <c>BankUnitDocument</c>). Vida atual não vem:
    /// para exibir, ver <see cref="DisplayedUnitStats"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (BankUnit unit in view.You.Bank) DrawUnit(unit.Card, unit.DamageTaken);
    /// </code>
    /// </example>
    public sealed class BankUnit
    {
        private BankUnit(IPayloadReader unit)
        {
            Card = MatchCard.Read(unit.ReadObject("card"));
            DamageTaken = unit.ReadInteger("damage_taken");
            Modifiers = UnitModifiers.ReadList(unit, "modifiers");
        }

        /// <summary>A carta da unidade.</summary>
        /// <example><code>CardInstanceId attacker = unit.Card.Instance;</code></example>
        public MatchCard Card { get; }

        /// <summary>Dano acumulado.</summary>
        /// <example><code>long damage = unit.DamageTaken;</code></example>
        public long DamageTaken { get; }

        /// <summary>Modificadores, na ordem do servidor.</summary>
        /// <example><code>int count = unit.Modifiers.Count;</code></example>
        public IReadOnlyList<UnitModifier> Modifiers { get; }

        /// <summary>Lê um objeto do banco.</summary>
        /// <example><code>BankUnit unit = BankUnit.Read(item);</code></example>
        internal static BankUnit Read(IPayloadReader unit) => new BankUnit(unit);

        /// <summary>Lê a lista <c>bank</c>, na ordem.</summary>
        /// <example><code>IReadOnlyList&lt;BankUnit&gt; bank = BankUnit.ReadList(side, "bank");</code></example>
        internal static IReadOnlyList<BankUnit> ReadList(IPayloadReader parent, string field)
        {
            return parent.ReadObjectList(field).Select(Read).ToArray();
        }
    }
}
