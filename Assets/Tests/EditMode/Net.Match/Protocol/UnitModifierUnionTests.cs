#nullable enable
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US1-4, US1-5, FR-004, SC-002: os três modificadores e os desconhecidos.</summary>
    public class UnitModifierUnionTests
    {
        [Test]
        public void TresModificadoresViramTiposDistintos()
        {
            BankUnit unit = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-declaration.json").View.You.Bank[0];

            AttackModifier attack = (AttackModifier)unit.Modifiers[0];
            HealthModifier health = (HealthModifier)unit.Modifiers[1];
            Assert.That((attack.Amount, attack.Duration), Is.EqualTo((3L, SpellDuration.UntilEndOfRound)));
            Assert.That((health.Amount, health.Duration), Is.EqualTo((1L, SpellDuration.Permanent)));
            Assert.That(unit.Modifiers[2], Is.InstanceOf<DamageImmunityModifier>());
            Assert.That(unit.Modifiers.Select(modifier => modifier.KindText), Is.EqualTo(new[] { "attack", "health", "damage_immunity" }));
        }

        [Test]
        public void KindDesconhecidoPreservaTextoEDuracao()
        {
            UnitModifier modifier = UnitModifiers.ReadOne(MatchJson.Reader("{\"modifier_kind\": \"poison\", \"amount\": 2, \"duration\": \"permanent\"}"));

            Assert.That(modifier, Is.InstanceOf<UnrecognizedModifier>());
            Assert.That((modifier.KindText, modifier.Duration), Is.EqualTo(("poison", SpellDuration.Permanent)));
        }

        [Test]
        public void KindDesconhecidoSemDuracaoTemDuracaoDesconhecida()
        {
            UnitModifier modifier = UnitModifiers.ReadOne(MatchJson.Reader("{\"modifier_kind\": \"stun\"}"));

            Assert.That((modifier.Duration, modifier.DurationText), Is.EqualTo((SpellDuration.Unknown, string.Empty)));
        }

        [Test]
        public void DuracaoDesconhecidaPreservaOTexto()
        {
            UnitModifier modifier = UnitModifiers.ReadOne(MatchJson.Reader("{\"modifier_kind\": \"attack\", \"amount\": 1, \"duration\": \"until_end_of_turn\"}"));

            Assert.That((modifier.Duration, modifier.DurationText), Is.EqualTo((SpellDuration.Unknown, "until_end_of_turn")));
        }

        [Test]
        public void AtaqueSemAmountFalhaComCaminho()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => UnitModifiers.ReadOne(MatchJson.Reader("{\"modifier_kind\": \"attack\", \"duration\": \"permanent\"}")));

            Assert.That(error.Failure.Path, Does.EndWith("amount"));
        }
    }
}
