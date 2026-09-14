#nullable enable
using System.Linq;
using Anathema.Net.Account;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Ataque e vida para mostrar numa unidade: base do catálogo, mais os modificadores de ataque e de vida, menos o
    /// dano sofrido na vida. É só conveniência de exibição: nada no cliente decide jogada, dano ou morte por estes
    /// números, que podem sair zero ou negativos sem o cliente concluir nada. Quem aplica dano é o servidor.
    /// </summary>
    /// <example>
    /// <code>
    /// DisplayedUnitStats? stats = DisplayedUnitStats.Of(unit, catalog);
    /// if (stats != null) statsLabel.text = $"{stats.Attack}/{stats.Health}";
    /// </code>
    /// </example>
    public sealed class DisplayedUnitStats
    {
        private DisplayedUnitStats(long attack, long health)
        {
            Attack = attack;
            Health = health;
        }

        /// <summary>Ataque exibido.</summary>
        /// <example><code>long attack = stats.Attack;</code></example>
        public long Attack { get; }

        /// <summary>Vida exibida.</summary>
        /// <example><code>long health = stats.Health;</code></example>
        public long Health { get; }

        /// <summary>Os números da unidade; nulo quando o <c>card_id</c> não é unidade no catálogo.</summary>
        /// <example><code>DisplayedUnitStats? stats = DisplayedUnitStats.Of(view.You.Bank[0], catalog);</code></example>
        public static DisplayedUnitStats? Of(BankUnit unit, LoadedCatalog catalog)
        {
            UnitCard? card = catalog.Find(unit.Card.Card).Unit;
            if (card == null)
                return null;

            long attack = card.Attack + unit.Modifiers.OfType<AttackModifier>().Sum(modifier => modifier.Amount);
            long health = card.Health + unit.Modifiers.OfType<HealthModifier>().Sum(modifier => modifier.Amount) - unit.DamageTaken;
            return new DisplayedUnitStats(attack, health);
        }
    }
}
