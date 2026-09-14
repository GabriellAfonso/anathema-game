#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class CatalogReaderTests
    {
        [Test]
        public void UnidadeEFeiticoSaoTiposDistintosComSeusCampos()
        {
            LoadedCatalog catalog = Load(new FakeClientLog(), CatalogJson.Unit(1), CatalogJson.Spell(1004));

            CardLookup unit = catalog.Find(new CardId(1));
            CardLookup spell = catalog.Find(new CardId(1004));

            Assert.That(unit.Kind, Is.EqualTo(CardLookupKind.Unit));
            Assert.That(unit.Unit!.Attack, Is.EqualTo(7));
            Assert.That(unit.Unit.Health, Is.EqualTo(5));
            Assert.That(unit.Unit.ImageKey, Is.EqualTo("john_card"));
            Assert.That(spell.Kind, Is.EqualTo(CardLookupKind.Spell));
            Assert.That(spell.Spell!.Description, Is.EqualTo("Você recupera 5 de Nexus."));
            AssertEffect(spell.Spell.Effect, false, SpellTargetKind.None, SpellDuration.Permanent);
        }

        [Test]
        public void MiraParaUnidadeInimigaEAteOFimDaRodada()
        {
            LoadedCatalog catalog = Load(new FakeClientLog(), CatalogJson.Spell(1002, "enemy_unit", "until_end_of_round", requiresTarget: true));

            AssertEffect(catalog.Find(new CardId(1002)).Spell!.Effect, true, SpellTargetKind.EnemyUnit, SpellDuration.UntilEndOfRound);
        }

        [Test]
        public void CartaForaDoCatalogoEhNaoEncontrada()
        {
            CardLookup missing = Load(new FakeClientLog(), CatalogJson.Unit(1)).Find(new CardId(9999));

            Assert.That(missing.Kind, Is.EqualTo(CardLookupKind.NotFound));
            Assert.That(missing.Requested, Is.EqualTo(new CardId(9999)));
            Assert.That(missing.Unit, Is.Null);
            Assert.That(missing.Spell, Is.Null);
        }

        [Test]
        public void ValoresDeEfeitoForaDoConjuntoFicamDesconhecidosComTexto()
        {
            FakeClientLog log = new FakeClientLog();

            SpellEffect effect = Load(log, CatalogJson.Spell(1003, "any_unit", "forever")).Find(new CardId(1003)).Spell!.Effect;

            Assert.That(effect.TargetKind, Is.EqualTo(SpellTargetKind.Unknown));
            Assert.That(effect.TargetKindText, Is.EqualTo("any_unit"));
            Assert.That(effect.Duration, Is.EqualTo(SpellDuration.Unknown));
            Assert.That(effect.DurationText, Is.EqualTo("forever"));
            Assert.That(log.Entries.Count(entry => entry.EventName == "catalog_effect_value_unknown"), Is.EqualTo(2));
        }

        [Test]
        public void CartaRuimEhOmitidaEORestoContinua()
        {
            FakeClientLog log = new FakeClientLog();
            string unitWithoutHealth = CatalogJson.Unit(2, "\"attack\": 3");
            string relic = "{\"card_id\": 3, \"card_type\": \"relic\"}";

            LoadedCatalog catalog = Load(log, CatalogJson.Unit(1), unitWithoutHealth, relic, CatalogJson.Spell(1004));

            Assert.That(catalog.Cards.Select(card => card.Card.Value), Is.EqualTo(new long[] { 1, 1004 }));
            ClientLogEntry[] skipped = log.Entries.Where(entry => entry.EventName == "catalog_card_skipped").ToArray();
            Assert.That(skipped.Length, Is.EqualTo(2));
            Assert.That(Field(skipped[0], "field"), Does.EndWith("health"));
            Assert.That(Field(skipped[1], "reason"), Is.EqualTo("unknown_card_type"));
            Assert.That(Field(skipped[1], "card_type"), Is.EqualTo("relic"));
        }

        [Test]
        public void CardIdRepetidoMantemOPrimeiro()
        {
            FakeClientLog log = new FakeClientLog();

            LoadedCatalog catalog = Load(log, CatalogJson.Unit(1), CatalogJson.Unit(1, "\"attack\": 99, \"health\": 99"));

            Assert.That(catalog.Cards.Count, Is.EqualTo(1));
            Assert.That(catalog.Find(new CardId(1)).Unit!.Attack, Is.EqualTo(7));
            log.Single("catalog_card_duplicated");
        }

        [TestCase("{}")]
        [TestCase("{\"cards\": {\"card_id\": 1}}")]
        public void SemListaDeCartasALeituraInteiraFalha(string body)
        {
            Assert.Throws<PayloadShapeException>(() => new CatalogReader(new FakeClientLog()).Read(AccountTestCodec.Reader(body)));
        }

        private static LoadedCatalog Load(FakeClientLog log, params string[] items)
        {
            IReadOnlyList<CatalogCard> cards = new CatalogReader(log).Read(AccountTestCodec.Reader(CatalogJson.Body(items)));
            return new LoadedCatalog(cards, 1);
        }

        private static void AssertEffect(SpellEffect effect, bool requiresTarget, SpellTargetKind target, SpellDuration duration)
        {
            Assert.That(effect.RequiresTarget, Is.EqualTo(requiresTarget));
            Assert.That(effect.TargetKind, Is.EqualTo(target));
            Assert.That(effect.Duration, Is.EqualTo(duration));
            Assert.That(effect.DeclarationOnly, Is.False);
        }

        private static string Field(ClientLogEntry entry, string name) => entry.Fields.Single(field => field.Name == name).Value;
    }
}
