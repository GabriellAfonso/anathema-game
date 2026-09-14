#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US1-2, US1-3, FR-006, SC-002: os 19 eventos, os detalhes e o desconhecido.</summary>
    public class MatchEventUnionTests
    {
        private static readonly UserId Self = new UserId(7);
        private static readonly UserId Rival = new UserId(9);

        private IReadOnlyList<MatchEvent> events = null!;

        [SetUp]
        public void ReadAllEvents()
        {
            events = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-all-events.json").Events;
        }

        [Test]
        public void DezenoveTiposNaOrdemDaLista()
        {
            Type[] expected =
            {
                typeof(MulliganTakenEvent), typeof(UnitPlayedEvent), typeof(SpellCastEvent), typeof(PassedEvent), typeof(AttackersSentEvent),
                typeof(AttackerWithdrawnEvent), typeof(AttackConfirmedEvent), typeof(BlockerAssignedEvent), typeof(BlockerRemovedEvent),
                typeof(DefenseEndedEvent), typeof(ForfeitedEvent), typeof(UnitDamagedEvent), typeof(UnitDiedEvent), typeof(NexusChangedEvent),
                typeof(RoundStartedEvent), typeof(CardsDrawnEvent), typeof(MatchFinishedEvent), typeof(TurnTimedOutEvent), typeof(MulliganTimedOutEvent),
            };

            Assert.That(events.Select(item => item.GetType()), Is.EqualTo(expected));
        }

        [Test]
        public void EventosDeJogadaTemCamposTipados()
        {
            Assert.That(At<MulliganTakenEvent>(0).SwappedCount, Is.EqualTo(1));
            Assert.That(At<UnitPlayedEvent>(1).Card, Is.EqualTo(new MatchCard(new CardInstanceId(21), new CardId(5))));
            Assert.That((At<SpellCastEvent>(2).User, At<SpellCastEvent>(2).Target), Is.EqualTo((Self, (CardInstanceId?)new CardInstanceId(40))));
            Assert.That(At<PassedEvent>(3).User, Is.EqualTo(Rival));
            Assert.That(At<AttackersSentEvent>(4).Attackers, Is.EqualTo(new[] { new CardInstanceId(21), new CardInstanceId(22) }));
            Assert.That(At<AttackerWithdrawnEvent>(5).Attacker, Is.EqualTo(new CardInstanceId(22)));
            Assert.That(At<AttackConfirmedEvent>(6).User, Is.EqualTo(Self));
            Assert.That((At<BlockerAssignedEvent>(7).Blocker, At<BlockerAssignedEvent>(7).Attacker), Is.EqualTo((new CardInstanceId(40), new CardInstanceId(21))));
            Assert.That(At<BlockerRemovedEvent>(8).Blocker, Is.EqualTo(new CardInstanceId(40)));
            Assert.That((At<DefenseEndedEvent>(9).User, At<ForfeitedEvent>(10).User), Is.EqualTo((Rival, Rival)));
        }

        [Test]
        public void ConsequenciasTemCamposTipados()
        {
            Assert.That((At<UnitDamagedEvent>(11).Unit, At<UnitDamagedEvent>(11).Amount), Is.EqualTo((new CardInstanceId(40), 3L)));
            Assert.That((At<UnitDiedEvent>(12).Owner, At<UnitDiedEvent>(12).Card.Card), Is.EqualTo((Rival, new CardId(1))));
            Assert.That(At<NexusChangedEvent>(13).Amount, Is.EqualTo(17));
            Assert.That((At<RoundStartedEvent>(14).RoundNumber, At<RoundStartedEvent>(14).TokenHolder), Is.EqualTo((4L, Rival)));
            MatchOutcome outcome = At<MatchFinishedEvent>(16).Outcome;
            Assert.That((outcome.DefeatedUser, outcome.Reason), Is.EqualTo((Rival, MatchEndReason.Forfeit)));
            Assert.That((At<TurnTimedOutEvent>(17).User, At<TurnTimedOutEvent>(17).TurnNumber), Is.EqualTo((Rival, 12L)));
            Assert.That(At<MulliganTimedOutEvent>(18).User, Is.EqualTo(Rival));
        }

        [Test]
        public void ComprasDoOponenteVemComContagemESemCartas()
        {
            CardsDrawnEvent drawn = At<CardsDrawnEvent>(15);

            Assert.That((drawn.User, drawn.Count, drawn.Cards.Count), Is.EqualTo((Rival, 1L, 0)));
        }

        [Test]
        public void DetalhesUsamOsNomesDosCamposDoContrato()
        {
            string[][] expected =
            {
                new[] { "user_id", "swapped_count" }, new[] { "user_id", "card" }, new[] { "user_id", "card", "target_card_instance_id" },
                new[] { "user_id" }, new[] { "user_id", "attacker_card_instance_ids" }, new[] { "user_id", "attacker_card_instance_id" },
                new[] { "user_id" }, new[] { "user_id", "blocker_card_instance_id", "attacker_card_instance_id" },
                new[] { "user_id", "blocker_card_instance_id" }, new[] { "user_id" }, new[] { "user_id" },
                new[] { "card_instance_id", "amount" }, new[] { "user_id", "card" }, new[] { "user_id", "amount" },
                new[] { "round_number", "token_holder_user_id" }, new[] { "user_id", "count", "cards" },
                new[] { "defeated_user_id", "reason" }, new[] { "user_id", "turn_number" }, new[] { "user_id" },
            };

            Assert.That(events.Select(item => item.Details.Select(detail => detail.Field).ToArray()), Is.EqualTo(expected));
        }

        [Test]
        public void KindTextoDeCadaEventoEhODoContrato()
        {
            Assert.That(events.Select(item => item.KindText).Distinct().Count(), Is.EqualTo(19));
            Assert.That(events[0].KindText, Is.EqualTo("mulligan_taken"));
        }

        [Test]
        public void FeiticoSemAlvoTemAlvoNuloENoneNoDetalhe()
        {
            string json = Update("{\"kind\": \"spell_cast\", \"user_id\": 7, \"card\": {\"card_instance_id\": 23, \"card_id\": 1003}, \"target_card_instance_id\": null}");

            SpellCastEvent cast = (SpellCastEvent)MatchJson.Frame<MatchUpdateFrame>(json).Events.Single();

            Assert.That(cast.Target, Is.Null);
            Assert.That(cast.Details[2].Text, Is.EqualTo("none"));
        }

        [Test]
        public void KindDesconhecidoViraEventoNaoReconhecido()
        {
            MatchEvent unknown = MatchJson.Frame<MatchUpdateFrame>(Update("{\"kind\": \"card_discarded\", \"user_id\": 7}")).Events.Single();

            Assert.That(unknown, Is.InstanceOf<UnrecognizedMatchEvent>());
            Assert.That((unknown.KindText, unknown.Details.Count), Is.EqualTo(("card_discarded", 0)));
        }

        [Test]
        public void UnidadeJogadaSemCartaInvalidaOFrame()
        {
            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode(Update("{\"kind\": \"unit_played\", \"user_id\": 7}"));

            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Failure.Path, Does.EndWith("events[0].card"));
        }

        private T At<T>(int index) where T : MatchEvent => (T)events[index];

        private static string Update(string singleEvent)
        {
            string text = MatchFixtures.Text("contract-match-update-no-clock.json");
            return text.Replace("{\"kind\": \"passed\", \"user_id\": 9}", singleEvent);
        }
    }
}
