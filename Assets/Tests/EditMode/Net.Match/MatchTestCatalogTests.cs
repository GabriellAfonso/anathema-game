#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    public class MatchTestCatalogTests
    {
        [Test]
        public void CatalogoPadraoTemUnidadeComAtaqueEVida()
        {
            CardLookup unit = MatchTestCatalog.Default().Find(new CardId(MatchTestCatalog.SandWolf));

            Assert.That(unit.Kind, Is.EqualTo(CardLookupKind.Unit));
            Assert.That((unit.Unit!.Attack, unit.Unit.Health, unit.Unit.Energy), Is.EqualTo((3L, 4L, 3L)));
        }

        [Test]
        public void CatalogoPadraoTemFeiticoComMiraESoNaDeclaracao()
        {
            LoadedCatalog catalog = MatchTestCatalog.Default();

            Assert.That(catalog.Find(new CardId(MatchTestCatalog.Fireball)).Spell!.Effect.TargetKind, Is.EqualTo(SpellTargetKind.EnemyUnit));
            Assert.That(catalog.Find(new CardId(MatchTestCatalog.SacrificialFire)).Spell!.Effect.DeclarationOnly, Is.True);
        }
    }
}
