#nullable enable
using System;
using System.IO;
using System.Linq;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class RecordingWebSocketFactoryTests
    {
        private string directory = string.Empty;

        [SetUp]
        public void CreateDirectory()
        {
            directory = Path.Combine(Path.GetTempPath(), "anathema-recording-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        }

        [TearDown]
        public void DeleteDirectory()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void GravaCadaTextoRecebidoNumeradoPeloTipo()
        {
            FakeWebSocketFactory fakes = new FakeWebSocketFactory();
            RecordingWebSocketFactory recording = new RecordingWebSocketFactory(fakes, directory);

            recording.Create().Open(new Uri("ws://127.0.0.1:8000/ws/match/"));
            fakes.Latest.SimulateOpened();
            fakes.Latest.SimulateText("{\"type\": \"match_start\", \"payload\": {}}");
            fakes.Latest.SimulateText("{\"type\":\"pong\",\"payload\":{}}");

            string[] names = Directory.GetFiles(directory).Select(Path.GetFileName).OrderBy(name => name).ToArray()!;
            Assert.That(names, Is.EqualTo(new[] { "0001-match_start.json", "0002-pong.json" }));
            Assert.That(File.ReadAllText(Path.Combine(directory, names[0])), Is.EqualTo("{\"type\": \"match_start\", \"payload\": {}}"));
        }

        [Test]
        public void DevolveOProprioSocketDaFabricaReal()
        {
            FakeWebSocketFactory fakes = new FakeWebSocketFactory();

            Assert.That(new RecordingWebSocketFactory(fakes, directory).Create(), Is.SameAs(fakes.Latest));
        }

        [TestCase("{\"type\": \"turn_warning\"}", "turn_warning")]
        [TestCase("sem tipo", "unknown")]
        public void TipoLidoDoTexto(string text, string expected)
        {
            Assert.That(RecordingWebSocketFactory.TypeOf(text), Is.EqualTo(expected));
        }
    }
}
