#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>A estratégia do marco sobre as fixtures de contrato da partida (research R14).</summary>
    public class SmokeStrategyTests
    {
        private IProtocolCodec codec = null!;

        [SetUp]
        public void CreateCodec()
        {
            codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), new FakeClientLog());
        }

        [Test]
        public void P1TrocaAPrimeiraCartaUmaVez()
        {
            SmokeStrategy strategy = Strategy("P1");
            PlayerView view = View("contract-match-start-mulligan.json");

            MulliganCommand mulligan = (MulliganCommand)strategy.Next(view, 4)!;

            Assert.That(mulligan.Swapped, Is.EqualTo(new[] { new CardInstanceId(1) }));
            Assert.That(strategy.Next(view, 4), Is.Null);
        }

        [Test]
        public void P2NaoTrocaNada()
        {
            MulliganCommand mulligan = (MulliganCommand)Strategy("P2").Next(View("contract-match-start-mulligan.json"), 4)!;

            Assert.That(mulligan.Swapped, Is.Empty);
        }

        [Test]
        public void SemPrioridadeNaoAge()
        {
            Assert.That(Strategy("P1").Next(View("contract-match-update-action.json"), 5), Is.Null);
        }

        [Test]
        public void RodadaTrintaDesiste()
        {
            PlayerView view = View("contract-match-update-action.json", ("\"round_number\": 2", "\"round_number\": 30"), ("\"priority_user_id\": 9", "\"priority_user_id\": 7"));

            Assert.That(Strategy("P1").Next(view, 5), Is.InstanceOf<ForfeitCommand>());
        }

        [Test]
        public void AcaoJogaAUnidadeMaisBarataEDepoisPassa()
        {
            SmokeStrategy strategy = Strategy("P1");
            PlayerView view = View("contract-match-update-action.json", ("\"priority_user_id\": 9", "\"priority_user_id\": 7"));

            Assert.That(((PlayUnitCommand)strategy.Next(view, 5)!).Card, Is.EqualTo(new CardInstanceId(1)));
            Assert.That(((PlayUnitCommand)strategy.Next(view, 5)!).Card, Is.EqualTo(new CardInstanceId(31)));
            Assert.That(strategy.Next(view, 5), Is.InstanceOf<PassCommand>());
        }

        [Test]
        public void DeclaracaoPuxaOUltimoUmaVezEDepoisConfirma()
        {
            SmokeStrategy strategy = Strategy("P1");
            PlayerView view = View("contract-match-update-declaration.json", ("\"card_id\": 1003", "\"card_id\": 1"));

            Assert.That(((WithdrawAttackerCommand)strategy.Next(view, 9)!).Attacker, Is.EqualTo(new CardInstanceId(22)));
            Assert.That(strategy.Next(view, 10), Is.InstanceOf<ConfirmAttackCommand>());
        }

        [Test]
        public void SemCandidatoNovoFalhaComFaseEVersao()
        {
            SmokeStrategy strategy = Strategy("P1");
            PlayerView view = View("contract-match-update-combat-blocked.json", ("\"priority_user_id\": 9", "\"priority_user_id\": 7"));

            Assert.That(strategy.Next(view, 11), Is.InstanceOf<EndDefenseWindowCommand>());
            InvalidOperationException stuck = Assert.Throws<InvalidOperationException>(() => strategy.Next(view, 11));
            Assert.That(stuck.Message, Does.Contain("Combat").And.Contain("11"));
        }

        private SmokeStrategy Strategy(string label) => new SmokeStrategy(label, SmokeCatalog.Default(), codec);

        private PlayerView View(string name, params (string Find, string Replace)[] edits)
        {
            string text = File.ReadAllText(Path.Combine(FixturesDirectory(), name));
            foreach ((string find, string replace) in edits)
                text = text.Replace(find, replace);

            ServerFrame frame = codec.Decode(text).Value;
            return frame is MatchStartFrame start ? start.View : ((MatchUpdateFrame)frame).View;
        }

        private static string FixturesDirectory([CallerFilePath] string sourcePath = "")
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "..", "Net.Match", "Fixtures");
        }
    }
}
