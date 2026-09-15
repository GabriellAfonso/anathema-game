#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>A estratégia da prova sobre as fixtures de contrato da partida (research R14 da 004, R11 da 005).</summary>
    public class ProofStrategyTests
    {
        private static readonly (string, string) MyPriority = ("\"priority_user_id\": 9", "\"priority_user_id\": 7");

        private LoadedCatalog catalog = null!;
        private CommandCoverage coverage = null!;
        private PlayerView? current;

        [SetUp]
        public void CreateCatalog()
        {
            catalog = ProofCatalog.Default();
            coverage = new CommandCoverage();
            current = null;
        }

        [Test]
        public void P1TrocaAPrimeiraCartaUmaVez()
        {
            ProofStrategy strategy = Strategy("P1");
            PlayerView view = ProofFixtures.View("contract-match-start-mulligan.json");

            MulliganCommand mulligan = (MulliganCommand)Next(strategy, view, 4)!;

            Assert.That(mulligan.Swapped, Is.EqualTo(new[] { new CardInstanceId(1) }));
            Assert.That(Next(strategy, view, 4), Is.Null);
        }

        [Test]
        public void P2NaoTrocaNada()
        {
            MulliganCommand mulligan = (MulliganCommand)Next(Strategy("P2"), ProofFixtures.View("contract-match-start-mulligan.json"), 4)!;

            Assert.That(mulligan.Swapped, Is.Empty);
        }

        [Test]
        public void DesistenciaNoMulliganNaPartidaDois()
        {
            ProofStrategy strategy = Strategy("P1");
            strategy.ForfeitsInMulligan = true;
            PlayerView view = ProofFixtures.View("contract-match-start-mulligan.json");

            Assert.That(Next(strategy, view, 4), Is.InstanceOf<ForfeitCommand>());
            Assert.That(Next(strategy, view, 4), Is.Null);
        }

        [Test]
        public void SemPrioridadeNaoAge()
        {
            Assert.That(Next(Strategy("P1"), ProofFixtures.View("contract-match-update-action.json"), 5), Is.Null);
        }

        [Test]
        public void RodadaTrintaDesiste()
        {
            PlayerView view = ProofFixtures.View("contract-match-update-action.json", ("\"round_number\": 2", "\"round_number\": 30"), MyPriority);

            Assert.That(Next(Strategy("P1"), view, 5), Is.InstanceOf<ForfeitCommand>());
        }

        [Test]
        public void AcaoJogaAUnidadeMaisBarataEDepoisPassa()
        {
            ProofStrategy strategy = Strategy("P1");
            PlayerView view = ProofFixtures.View("contract-match-update-action.json", MyPriority);

            Assert.That(((PlayUnitCommand)Next(strategy, view, 5)!).Card, Is.EqualTo(new CardInstanceId(1)));
            Assert.That(((PlayUnitCommand)Next(strategy, view, 5)!).Card, Is.EqualTo(new CardInstanceId(31)));
            Assert.That(Next(strategy, view, 5), Is.InstanceOf<PassCommand>());
        }

        [Test]
        public void DeclaracaoPuxaOUltimoUmaVezEDepoisConfirma()
        {
            ProofStrategy strategy = Strategy("P1");
            PlayerView view = ProofFixtures.View("contract-match-update-declaration.json", ("\"card_id\": 1003", "\"card_id\": 1"));

            Assert.That(((WithdrawAttackerCommand)Next(strategy, view, 9)!).Attacker, Is.EqualTo(new CardInstanceId(22)));
            Assert.That(Next(strategy, view, 10), Is.InstanceOf<ConfirmAttackCommand>());
        }

        [Test]
        public void SemCandidatoNovoFalhaComFaseEVersao()
        {
            ProofStrategy strategy = Strategy("P1");
            PlayerView view = ProofFixtures.View("contract-match-update-combat-blocked.json", MyPriority);

            Assert.That(Next(strategy, view, 11), Is.InstanceOf<EndDefenseWindowCommand>());
            for (int repeat = 0; repeat < 3; repeat++)
                Assert.That(Next(strategy, view, 11), Is.InstanceOf<EndDefenseWindowCommand>(), "gatilho repetido na mesma versão repete o último candidato");

            InvalidOperationException stuck = Assert.Throws<InvalidOperationException>(() => Next(strategy, view, 11));
            Assert.That(stuck.Message, Does.Contain("Combat").And.Contain("11"));
        }

        [Test]
        public void DepoisDaVoltaAAoVivoTentaDeNovoNaMesmaVersao()
        {
            ProofStrategy strategy = Strategy("P1");
            PlayerView view = ProofFixtures.View("contract-match-update-declaration.json", ("\"card_id\": 1003", "\"card_id\": 1"));
            Next(strategy, view, 25);
            Assert.That(Next(strategy, view, 25), Is.InstanceOf<ConfirmAttackCommand>());

            strategy.ForgetTried();

            Assert.That(Next(strategy, view, 25), Is.InstanceOf<ConfirmAttackCommand>(), "o puxar atacante continua valendo uma vez só");
        }

        [Test]
        public void DefesaRemoveOProprioBloqueadorUmaVez()
        {
            PlayerView view = OwnBlockView();

            Assert.That(((RemoveBlockerCommand)Next(Strategy("P2"), view, 11)!).Blocker, Is.EqualTo(new CardInstanceId(21)));
            Cover(new RemoveBlockerCommand(new CardInstanceId(21)), ProofFixtures.Event<BlockerRemovedEvent>(), new UserId(9));
            Assert.That(Next(Strategy("P2"), view, 12), Is.InstanceOf<EndDefenseWindowCommand>());
        }

        [Test]
        public void BloqueioDoAdversarioNaoERemovido()
        {
            PlayerView view = ProofFixtures.View("contract-match-update-combat-blocked.json", MyPriority);

            Assert.That(Next(Strategy("P2"), view, 11), Is.InstanceOf<EndDefenseWindowCommand>());
        }

        [Test]
        public void PocaoIgnoraOTetoDeNexusAteOFeiticoSemAlvoSerCoberto()
        {
            PlayerView view = PotionInHandView();

            CastSpellCommand cast = (CastSpellCommand)Next(Strategy("P1"), view, 5)!;
            Assert.That((cast.Card, cast.Target), Is.EqualTo((new CardInstanceId(1), (CardInstanceId?)null)));

            Cover(new CastSpellCommand(new CardInstanceId(23), null), ProofFixtures.Event<SpellCastEvent>((", \"target_card_instance_id\": 40", string.Empty)), new UserId(7));
            Assert.That(((PlayUnitCommand)Next(Strategy("P1"), view, 5)!).Card, Is.EqualTo(new CardInstanceId(31)));
        }

        [Test]
        public void ComCombateProvadoEFeiticoSemAlvoFaltandoNaoAtaca()
        {
            PlayerView view = ProofFixtures.View("contract-match-update-action.json", MyPriority, ("\"token_holder_user_id\": 9", "\"token_holder_user_id\": 7"));
            Assert.That(Next(Strategy("P1"), view, 5), Is.InstanceOf<DeclareAttackCommand>(), "antes de provar o combate, ataca");

            CoverCombat();

            Assert.That(((PlayUnitCommand)Next(Strategy("P1"), view, 5)!).Card, Is.EqualTo(new CardInstanceId(1)));
            Cover(new CastSpellCommand(new CardInstanceId(23), null), ProofFixtures.Event<SpellCastEvent>((", \"target_card_instance_id\": 40", string.Empty)), new UserId(7));
            Assert.That(Next(Strategy("P1"), view, 5), Is.InstanceOf<DeclareAttackCommand>(), "com o feitiço sem alvo provado, volta a atacar");
        }

        [Test]
        public void P2FicaParadoNaVezEscolhidaEVoltaAAgirQuandoElaEstoura()
        {
            ProofStrategy strategy = Strategy("P2");
            strategy.StallFromRound = 2;
            PlayerView view = ProofFixtures.View("contract-match-update-action.json", MyPriority);

            Assert.That(Next(strategy, view, 5), Is.Null);
            Assert.That(Next(strategy, view, 6), Is.Null, "a volta de uma queda na mesma vez continua parada");
            Assert.That(strategy.IsStalling, Is.True);

            strategy.NoteTurnTimedOut();

            Assert.That(strategy.IsStalling, Is.False);
            Assert.That(Next(strategy, view, 7), Is.InstanceOf<PlayUnitCommand>());
        }

        [Test]
        public void AntesDaRodadaDaVezParadaAgeNormalmente()
        {
            ProofStrategy strategy = Strategy("P2");
            strategy.StallFromRound = 3;

            Assert.That(Next(strategy, ProofFixtures.View("contract-match-update-action.json", MyPriority), 5), Is.InstanceOf<PlayUnitCommand>());
            Assert.That(strategy.IsStalling, Is.False);
        }

        private ProofStrategy Strategy(string label) => new ProofStrategy(label, catalog, card => HandCardHints.For(card, current!, catalog), coverage);

        private PlayCommand? Next(ProofStrategy strategy, PlayerView view, long version)
        {
            current = view;
            return strategy.Next(view, version);
        }

        private void Cover(PlayCommand sent, MatchEvent confirmed, UserId self)
        {
            coverage.NoteSent("P", sent);
            coverage.NoteEvent("P", self, confirmed);
        }

        private void CoverCombat()
        {
            UserId attacker = new UserId(7), defender = new UserId(9);
            Cover(new DeclareAttackCommand(new[] { new CardInstanceId(21) }), ProofFixtures.Event<AttackersSentEvent>(), attacker);
            Cover(new WithdrawAttackerCommand(new CardInstanceId(22)), ProofFixtures.Event<AttackerWithdrawnEvent>(), attacker);
            Cover(new ConfirmAttackCommand(), ProofFixtures.Event<AttackConfirmedEvent>(), attacker);
            Cover(new AssignBlockerCommand(new CardInstanceId(40), new CardInstanceId(21)), ProofFixtures.Event<BlockerAssignedEvent>(), defender);
            Cover(new RemoveBlockerCommand(new CardInstanceId(40)), ProofFixtures.Event<BlockerRemovedEvent>(), defender);
            Cover(new EndDefenseWindowCommand(), ProofFixtures.Event<DefenseEndedEvent>(), defender);
        }

        private static PlayerView OwnBlockView()
        {
            return ProofFixtures.View("contract-match-update-combat-blocked.json", MyPriority, ("[21, 22]", "[40]"),
                ("\"blocker_card_instance_id\": 40, \"attacker_card_instance_id\": 21", "\"blocker_card_instance_id\": 21, \"attacker_card_instance_id\": 40"));
        }

        private static PlayerView PotionInHandView()
        {
            return ProofFixtures.View("contract-match-update-action.json", MyPriority,
                ("{\"card_instance_id\": 1, \"card_id\": 1}", "{\"card_instance_id\": 1, \"card_id\": " + ProofCatalog.PotionCard + "}"));
        }
    }
}
