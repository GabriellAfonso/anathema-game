#nullable enable
using Anathema.Net.Account;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US2-8, FR-016: atributos exibidos, só para mostrar.</summary>
    public class DisplayedUnitStatsTests
    {
        private LoadedCatalog catalog = null!;

        [SetUp]
        public void LoadCatalog()
        {
            catalog = MatchTestCatalog.Default();
        }

        [Test]
        public void BaseMaisModificadoresMenosDano()
        {
            BankUnit wolf = MirrorViews.Declaration().You.Bank[0];

            DisplayedUnitStats stats = DisplayedUnitStats.Of(wolf, catalog)!;

            Assert.That((stats.Attack, stats.Health), Is.EqualTo((6L, 3L)));
        }

        [Test]
        public void ImunidadeEModificadorDesconhecidoNaoMudamNumero()
        {
            DisplayedUnitStats stats = DisplayedUnitStats.Of(Unit(MatchTestCatalog.SandWolf, 0,
                "{\"modifier_kind\": \"damage_immunity\", \"duration\": \"permanent\"}, {\"modifier_kind\": \"poison\", \"amount\": 9, \"duration\": \"permanent\"}"), catalog)!;

            Assert.That((stats.Attack, stats.Health), Is.EqualTo((3L, 4L)));
        }

        [Test]
        public void DoisModificadoresDeAtaqueSomam()
        {
            DisplayedUnitStats stats = DisplayedUnitStats.Of(Unit(MatchTestCatalog.IronGuard, 5,
                "{\"modifier_kind\": \"attack\", \"amount\": 3, \"duration\": \"permanent\"}, {\"modifier_kind\": \"attack\", \"amount\": 2, \"duration\": \"until_end_of_round\"}"), catalog)!;

            Assert.That((stats.Attack, stats.Health), Is.EqualTo((7L, -2L)));
        }

        [TestCase(MatchTestCatalog.Fireball)]
        [TestCase(9999)]
        public void CartaQueNaoEhUnidadeNoCatalogoEhNula(long cardId)
        {
            Assert.That(DisplayedUnitStats.Of(Unit(cardId, 0, string.Empty), catalog), Is.Null);
        }

        private static BankUnit Unit(long cardId, long damage, string modifiers)
        {
            return BankUnit.Read(MatchJson.Reader("{\"card\": {\"card_instance_id\": 50, \"card_id\": " + cardId + "}, \"damage_taken\": " + damage + ", \"modifiers\": [" + modifiers + "]}"));
        }
    }
}
