#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-008, SC-007: cada comando produz o envelope da tabela "Escrita" de contracts/protocol-shapes.md.</summary>
    public class PlayCommandEncodingTests
    {
        private static CardInstanceId Card(long value) => new CardInstanceId(value);

        [Test]
        public void MulliganComCartas()
        {
            AssertEncodes(new MulliganCommand(new[] { Card(3), Card(7) }), "{\"type\":\"mulligan\",\"payload\":{\"card_instance_ids\":[3,7]}}");
        }

        [Test]
        public void MulliganSemCartas()
        {
            AssertEncodes(new MulliganCommand(Array.Empty<CardInstanceId>()), "{\"type\":\"mulligan\",\"payload\":{\"card_instance_ids\":[]}}");
        }

        [Test]
        public void JogarUnidade()
        {
            AssertEncodes(new PlayUnitCommand(Card(12)), "{\"type\":\"play_unit\",\"payload\":{\"card_instance_id\":12}}");
        }

        [Test]
        public void FeiticoComAlvo()
        {
            AssertEncodes(new CastSpellCommand(Card(12), Card(40)), "{\"type\":\"cast_spell\",\"payload\":{\"card_instance_id\":12,\"target_card_instance_id\":40}}");
        }

        [Test]
        public void FeiticoSemAlvoOmiteOCampo()
        {
            AssertEncodes(new CastSpellCommand(Card(23), null), "{\"type\":\"cast_spell\",\"payload\":{\"card_instance_id\":23}}");
        }

        [Test]
        public void DeclararAtaque()
        {
            AssertEncodes(new DeclareAttackCommand(new[] { Card(21), Card(22) }), "{\"type\":\"declare_attack\",\"payload\":{\"attacker_card_instance_ids\":[21,22]}}");
        }

        [Test]
        public void PuxarAtacante()
        {
            AssertEncodes(new WithdrawAttackerCommand(Card(22)), "{\"type\":\"withdraw_attacker\",\"payload\":{\"attacker_card_instance_id\":22}}");
        }

        [Test]
        public void AtribuirBloqueador()
        {
            AssertEncodes(new AssignBlockerCommand(Card(4), Card(21)), "{\"type\":\"assign_blocker\",\"payload\":{\"blocker_card_instance_id\":4,\"attacker_card_instance_id\":21}}");
        }

        [Test]
        public void RemoverBloqueador()
        {
            AssertEncodes(new RemoveBlockerCommand(Card(4)), "{\"type\":\"remove_blocker\",\"payload\":{\"blocker_card_instance_id\":4}}");
        }

        [TestCase("pass")]
        [TestCase("confirm_attack")]
        [TestCase("end_defense_window")]
        [TestCase("forfeit")]
        public void ComandosSemCampoTemPayloadVazio(string type)
        {
            AssertEncodes(Empty(type), "{\"type\":\"" + type + "\",\"payload\":{}}");
        }

        [Test]
        public void ListasSaoCopiadasNaConstrucao()
        {
            CardInstanceId[] cards = { Card(3) };
            MulliganCommand mulligan = new MulliganCommand(cards);
            cards[0] = Card(99);

            Assert.That(mulligan.Swapped, Is.EqualTo(new[] { Card(3) }));
        }

        private static PlayCommand Empty(string type)
        {
            return type switch
            {
                PassCommand.TypeName => new PassCommand(),
                ConfirmAttackCommand.TypeName => new ConfirmAttackCommand(),
                EndDefenseWindowCommand.TypeName => new EndDefenseWindowCommand(),
                _ => new ForfeitCommand(),
            };
        }

        private static void AssertEncodes(PlayCommand command, string expected)
        {
            Assert.That(MatchJson.Encode(command), Is.EqualTo(expected));
        }
    }
}
