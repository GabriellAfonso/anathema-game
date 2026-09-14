#nullable enable
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-001, FR-009: a união decodifica os frames de partida e descarta o inválido inteiro.</summary>
    public class MatchFramesTests
    {
        [TestCase("contract-match-start-mulligan.json", typeof(MatchStartFrame))]
        [TestCase("contract-match-update-action.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-match-update-declaration.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-match-update-combat-blocked.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-match-update-finished.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-match-update-all-events.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-match-update-no-clock.json", typeof(MatchUpdateFrame))]
        [TestCase("contract-turn-warning.json", typeof(TurnWarningFrame))]
        public void CadaFixtureDecodificaNoTipoCerto(string name, System.Type expected)
        {
            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode(MatchFixtures.Text(name));

            Assert.That(decoded.IsValid, Is.True, name);
            Assert.That(decoded.Value, Is.InstanceOf(expected));
        }

        [Test]
        public void MatchStartTemVersaoVisaoERelogio()
        {
            MatchStartFrame start = MatchJson.Fixture<MatchStartFrame>("contract-match-start-mulligan.json");

            Assert.That((start.Version, start.View.Phase, start.Clock.MulliganRemainingMs), Is.EqualTo((4L, MatchPhase.Mulligan, (long?)30000)));
        }

        [Test]
        public void MatchUpdateTemEventosNaOrdemERelogio()
        {
            MatchUpdateFrame update = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-action.json");

            Assert.That(update.Version, Is.EqualTo(5));
            Assert.That(update.Events.Select(item => item.KindText), Is.EqualTo(new[] { "passed", "round_started", "cards_drawn", "cards_drawn" }));
            Assert.That(update.Clock.Turn!.TurnNumber, Is.EqualTo(12));
        }

        [Test]
        public void MatchUpdateSemFaseEhInvalidoComCaminhoESemFrameParcial()
        {
            string json = MatchFixtures.Text("contract-match-update-action.json").Replace("\"phase\": \"action\",", string.Empty);

            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode(json);

            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(decoded.Failure.Path, Does.EndWith("view.phase"));
        }

        [Test]
        public void GenericosEFilaContinuamNaUniao()
        {
            Assert.That(MatchJson.Decode("{\"type\": \"pong\", \"payload\": {}}").Value, Is.InstanceOf<PongFrame>());
            Assert.That(MatchJson.Decode("{\"type\": \"matchmaking_failed\", \"payload\": {\"error\": \"x\"}}").Value, Is.InstanceOf<Anathema.Net.Connection.MatchmakingFailedFrame>());
            Assert.That(MatchJson.Decode("{\"type\": \"message_refused\", \"payload\": {\"code\": \"bank_is_full\", \"error\": \"x\"}}").Value, Is.InstanceOf<MessageRefusedFrame>());
        }
    }
}
