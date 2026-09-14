#nullable enable
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>
    /// Frames reais gravados pelo marco LiveServer (quickstart §3): cada um decodifica, e nenhum <c>match_update</c>
    /// traz evento que o cliente não conhece. Sem gravação copiada para <c>Fixtures/</c>, não há o que verificar.
    /// </summary>
    public class RecordedFramesTests
    {
        [Test]
        public void CadaFrameGravadoDecodificaSemEventoDesconhecido()
        {
            // O NUnit do Unity não tem Assert.Multiple: a mensagem de cada asserção carrega o nome do arquivo.
            foreach (string name in MatchFixtures.Names("recorded-"))
                AssertRecorded(name);
        }

        private static void AssertRecorded(string name)
        {
            DecodeOutcome<ServerFrame> decoded = MatchJson.Decode(MatchFixtures.Text(name));
            Assert.That(decoded.IsValid, Is.True, name);
            if (decoded.IsValid && decoded.Value is MatchUpdateFrame update)
                Assert.That(update.Events.OfType<UnrecognizedMatchEvent>().Select(item => item.KindText), Is.Empty, name);
        }
    }
}
