#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionFramesTests
    {
        [TestCase("{\"type\": \"pong\", \"payload\": {}}", typeof(PongFrame))]
        [TestCase("{\"type\": \"auth_denied\", \"payload\": {\"error\": \"x\"}}", typeof(AuthDeniedFrame))]
        [TestCase("{\"type\": \"match_denied\", \"payload\": {\"error\": \"x\"}}", typeof(MatchDeniedFrame))]
        [TestCase("{\"type\": \"message_refused\", \"payload\": {\"error\": \"x\", \"code\": \"deck_not_specified\"}}", typeof(MessageRefusedFrame))]
        [TestCase(TestFrames.MatchmakingFailed, typeof(MatchmakingFailedFrame))]
        [TestCase(TestFrames.MatchFound, typeof(MatchFoundFrame))]
        public void UniaoDaCamadaConheceOsFramesGenericosEOsDaFila(string json, Type expected)
        {
            DecodeOutcome<ServerFrame> decoded = ConnectionTestCodec.Codec().Decode(json);

            Assert.That(decoded.IsValid, Is.True);
            Assert.That(decoded.Value, Is.TypeOf(expected));
        }
    }
}
