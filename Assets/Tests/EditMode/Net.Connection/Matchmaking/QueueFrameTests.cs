#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Mensagem e frames do socket de fila: join_queue, match_found, matchmaking_failed.</summary>
    public class QueueFrameTests
    {
        [Test]
        public void JoinQueueSaiComODeckInteiro()
        {
            string text = ConnectionTestCodec.Codec().Encode(new JoinQueueMessage(new DeckId(4)));

            Assert.That(text, Is.EqualTo("{\"type\":\"join_queue\",\"payload\":{\"deck_id\":4}}"));
        }

        [Test]
        public void MatchFoundViraPareamentoTipado()
        {
            MatchPairing pairing = ((MatchFoundFrame)Decode(TestFrames.MatchFound).Value).Pairing;

            Assert.That(pairing.Match, Is.EqualTo(new MatchId("match-7")));
            Assert.That(pairing.Self.User, Is.EqualTo(new UserId(7)));
            Assert.That(pairing.Self.Nickname, Is.EqualTo("one"));
            Assert.That(pairing.Opponent.User, Is.EqualTo(new UserId(9)));
            Assert.That(pairing.Opponent.Icon, Is.EqualTo("knight"));
            Assert.That(pairing.Opponent.Level, Is.EqualTo(3));
        }

        [TestCase("{\"type\": \"match_found\", \"payload\": {\"self\": {\"user_id\": 7, \"nickname\": \"one\", \"icon\": \"default\", \"level\": 1}, \"match_id\": \"m\"}}", "opponent")]
        [TestCase("{\"type\": \"match_found\", \"payload\": {\"self\": {\"user_id\": \"7\", \"nickname\": \"one\", \"icon\": \"default\", \"level\": 1}, \"opponent\": {\"user_id\": 9, \"nickname\": \"two\", \"icon\": \"default\", \"level\": 1}, \"match_id\": \"m\"}}", "user_id")]
        [TestCase("{\"type\": \"match_found\", \"payload\": {\"self\": {\"user_id\": 7, \"nickname\": \"one\", \"icon\": \"default\", \"level\": 1}, \"opponent\": {\"user_id\": 9, \"nickname\": \"two\", \"icon\": \"default\", \"level\": 1}, \"match_id\": 5}}", "match_id")]
        public void MatchFoundQuebradoEhInvalidoComOCampo(string json, string field)
        {
            DecodeOutcome<ServerFrame> decoded = Decode(json);

            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Failure.Path, Does.Contain(field));
        }

        [Test]
        public void MatchFoundComCamposAMaisSaiIgual()
        {
            string withExtras = TestFrames.MatchFound.Replace("\"match_id\"", "\"server_hint\": [1, 2], \"match_id\"");

            MatchPairing pairing = ((MatchFoundFrame)Decode(withExtras).Value).Pairing;

            Assert.That(pairing.Match, Is.EqualTo(new MatchId("match-7")));
        }

        [Test]
        public void MatchmakingFailedLeOErro()
        {
            MatchmakingFailedFrame failed = (MatchmakingFailedFrame)Decode(TestFrames.MatchmakingFailed).Value;

            Assert.That(failed.Error, Is.EqualTo("no profile for one of the paired users"));
            Assert.That(Decode("{\"type\": \"matchmaking_failed\", \"payload\": {}}").IsValid, Is.False);
        }

        private static DecodeOutcome<ServerFrame> Decode(string json) => ConnectionTestCodec.Codec().Decode(json);
    }
}
