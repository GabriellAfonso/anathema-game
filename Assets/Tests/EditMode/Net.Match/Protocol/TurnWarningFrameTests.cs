#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US1-8: <c>turn_warning</c> tipado.</summary>
    public class TurnWarningFrameTests
    {
        [Test]
        public void AvisoTemVezDonoERestante()
        {
            TurnWarningFrame warning = MatchJson.Fixture<TurnWarningFrame>("contract-turn-warning.json");

            Assert.That((warning.TurnNumber, warning.Holder, warning.RemainingMs), Is.EqualTo((12L, new UserId(7), 15000L)));
            Assert.That(warning.MessageType, Is.EqualTo("turn_warning"));
        }

        [Test]
        public void AvisoSemVezEhInvalido()
        {
            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode("{\"type\": \"turn_warning\", \"payload\": {\"holder_user_id\": 7, \"remaining_ms\": 15000}}");

            Assert.That(decoded.IsValid, Is.False);
            Assert.That(decoded.Failure.Path, Does.EndWith("turn_number"));
        }
    }
}
