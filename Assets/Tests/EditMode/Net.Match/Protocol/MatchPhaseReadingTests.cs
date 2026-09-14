#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-003, SC-002: as 7 fases e a desconhecida.</summary>
    public class MatchPhaseReadingTests
    {
        [TestCase("mulligan", MatchPhase.Mulligan)]
        [TestCase("upkeep", MatchPhase.Upkeep)]
        [TestCase("action", MatchPhase.Action)]
        [TestCase("declaration", MatchPhase.Declaration)]
        [TestCase("combat", MatchPhase.Combat)]
        [TestCase("round_end", MatchPhase.RoundEnd)]
        [TestCase("finished", MatchPhase.Finished)]
        public void FaseConhecidaViraOValor(string text, MatchPhase expected)
        {
            Assert.That(MatchPhaseText.Parse(text), Is.EqualTo(expected));
        }

        [TestCase("setup")]
        [TestCase("Action")]
        [TestCase("")]
        public void FaseForaDoConjuntoEhDesconhecida(string text)
        {
            Assert.That(MatchPhaseText.Parse(text), Is.EqualTo(MatchPhase.Unknown));
        }

        [Test]
        public void VisaoComFaseDesconhecidaPreservaOTexto()
        {
            string json = MatchFixtures.Text("contract-match-update-no-clock.json").Replace("\"phase\": \"action\"", "\"phase\": \"setup\"");

            PlayerView view = MatchJson.Frame<MatchUpdateFrame>(json).View;

            Assert.That((view.Phase, view.PhaseText), Is.EqualTo((MatchPhase.Unknown, "setup")));
        }
    }
}
