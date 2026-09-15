#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>A cobertura dos comandos da partida 1 (contracts/match-proof.md, passo 14).</summary>
    public class CommandCoverageTests
    {
        private static readonly UserId Attacker = new UserId(7);
        private static readonly UserId Defender = new UserId(9);

        [Test]
        public void ComecaFaltandoOsDozeItens()
        {
            CommandCoverage coverage = new CommandCoverage();

            Assert.That(coverage.Missing.Count, Is.EqualTo(12));
            Assert.That(coverage.Missing, Does.Contain("MulliganSwapping").And.Contain("MulliganKeeping").And.Contain("RemoveBlocker").And.Contain("EndDefense"));
        }

        [Test]
        public void EnvioConfirmadoPeloEventoDoMesmoJogadorConta()
        {
            CommandCoverage coverage = new CommandCoverage();

            coverage.NoteSent("P1", new ConfirmAttackCommand());
            coverage.NoteEvent("P1", Attacker, ProofFixtures.Event<AttackConfirmedEvent>());

            Assert.That(coverage.Covers(CoverageItem.ConfirmAttack), Is.True);
            Assert.That(coverage.Missing, Has.No.Member("ConfirmAttack"));
        }

        [Test]
        public void EventoSemEnvioNaoConta()
        {
            CommandCoverage coverage = new CommandCoverage();

            coverage.NoteEvent("P2", Defender, ProofFixtures.Event<PassedEvent>());

            Assert.That(coverage.Covers(CoverageItem.Pass), Is.False);
        }

        [Test]
        public void EnvioRecusadoNaoConta()
        {
            CommandCoverage coverage = new CommandCoverage();
            PassCommand pass = new PassCommand();

            coverage.NoteSent("P2", pass);
            coverage.NoteRefused("P2", new PlayRefusal(PlayRefusalCode.NotYourPriority, "not_your_priority", "not your priority", pass));
            coverage.NoteEvent("P2", Defender, ProofFixtures.Event<PassedEvent>());

            Assert.That(coverage.Covers(CoverageItem.Pass), Is.False);
        }

        [Test]
        public void EventoDeOutroJogadorNaoConta()
        {
            CommandCoverage coverage = new CommandCoverage();

            coverage.NoteSent("P1", new PassCommand());
            coverage.NoteEvent("P1", Attacker, ProofFixtures.Event<PassedEvent>());

            Assert.That(coverage.Covers(CoverageItem.Pass), Is.False);
        }

        [Test]
        public void MulliganContaTrocandoESemTrocarSeparados()
        {
            CommandCoverage coverage = new CommandCoverage();

            coverage.NoteSent("P1", new MulliganCommand(new[] { new CardInstanceId(1) }));
            coverage.NoteEvent("P1", Attacker, ProofFixtures.Event<MulliganTakenEvent>());
            coverage.NoteSent("P2", new MulliganCommand(new CardInstanceId[0]));
            coverage.NoteEvent("P2", Attacker, ProofFixtures.Event<MulliganTakenEvent>(("\"swapped_count\": 1", "\"swapped_count\": 0")));

            Assert.That(coverage.Covers(CoverageItem.MulliganSwapping) && coverage.Covers(CoverageItem.MulliganKeeping), Is.True);
        }

        [Test]
        public void FeiticoComESemAlvoSaoItensDiferentes()
        {
            CommandCoverage coverage = new CommandCoverage();

            coverage.NoteSent("P1", new CastSpellCommand(new CardInstanceId(23), new CardInstanceId(40)));
            coverage.NoteEvent("P1", Attacker, ProofFixtures.Event<SpellCastEvent>());

            Assert.That(coverage.Covers(CoverageItem.SpellWithTarget), Is.True);
            Assert.That(coverage.Covers(CoverageItem.SpellWithoutTarget), Is.False);
        }
    }
}
