#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-005: o relógio do frame, com vez e mulligan nulos ou preenchidos, e ausente.</summary>
    public class ClockViewReadingTests
    {
        [Test]
        public void VezPreenchidaTemOsQuatroCampos()
        {
            ClockView clock = Read("{\"clock\": {\"turn\": {\"turn_number\": 12, \"holder_user_id\": 7, \"remaining_ms\": 25000, \"warning\": true}, \"mulligan_remaining_ms\": null}}");

            TurnView turn = clock.Turn!;
            Assert.That((turn.TurnNumber, turn.Holder, turn.RemainingMs, turn.Warning), Is.EqualTo((12L, new UserId(7), 25000L, true)));
            Assert.That(clock.MulliganRemainingMs, Is.Null);
        }

        [Test]
        public void MulliganSemVez()
        {
            ClockView clock = Read("{\"clock\": {\"turn\": null, \"mulligan_remaining_ms\": 30000}}");

            Assert.That(clock.Turn, Is.Null);
            Assert.That(clock.MulliganRemainingMs, Is.EqualTo(30000));
        }

        [Test]
        public void OsDoisNulos()
        {
            ClockView clock = Read("{\"clock\": {\"turn\": null, \"mulligan_remaining_ms\": null}}");

            Assert.That((clock.Turn, clock.MulliganRemainingMs), Is.EqualTo(((TurnView?)null, (long?)null)));
        }

        [Test]
        public void OsDoisPreenchidosSaoLidosComoVieram()
        {
            ClockView clock = Read("{\"clock\": {\"turn\": {\"turn_number\": 1, \"holder_user_id\": 9, \"remaining_ms\": 1, \"warning\": false}, \"mulligan_remaining_ms\": 5}}");

            Assert.That((clock.Turn!.TurnNumber, clock.MulliganRemainingMs), Is.EqualTo((1L, (long?)5)));
        }

        [Test]
        public void RelogioAusenteEhVazio()
        {
            Assert.That(Read("{}"), Is.SameAs(ClockView.Empty));
            Assert.That(MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-no-clock.json").Clock, Is.SameAs(ClockView.Empty));
        }

        [Test]
        public void VezSemHolderFalhaComCaminho()
        {
            PayloadShapeException error = Assert.Throws<PayloadShapeException>(() => Read("{\"clock\": {\"turn\": {\"turn_number\": 1, \"remaining_ms\": 1, \"warning\": false}, \"mulligan_remaining_ms\": null}}"));

            Assert.That(error.Failure.Path, Does.EndWith("clock.turn.holder_user_id"));
        }

        private static ClockView Read(string json) => ClockView.ReadOptional(MatchJson.Reader(json));
    }
}
