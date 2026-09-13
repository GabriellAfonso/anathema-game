#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class LaunchServerHostReaderTests
    {
        [Test]
        public void ValorQueSegueAFlag()
        {
            string[] arguments = { "Anathema.exe", "-serverHost", "192.168.0.10:8000" };

            Assert.That(LaunchServerHostReader.FindCommandLineValue(arguments, "-serverHost"), Is.EqualTo("192.168.0.10:8000"));
        }

        [Test]
        public void FlagSemValorOuAusenteEhNulo()
        {
            Assert.That(LaunchServerHostReader.FindCommandLineValue(new[] { "Anathema.exe", "-serverHost" }, "-serverHost"), Is.Null);
            Assert.That(LaunchServerHostReader.FindCommandLineValue(new[] { "Anathema.exe", "-serverHost", "-batchmode" }, "-serverHost"), Is.Null);
            Assert.That(LaunchServerHostReader.FindCommandLineValue(new[] { "Anathema.exe" }, "-serverHost"), Is.Null);
        }

        [Test]
        public void ProducaoNaoLe()
        {
            Assert.That(LaunchServerHostReader.ShouldRead(developmentBuild: false), Is.False);
            Assert.That(LaunchServerHostReader.ShouldRead(developmentBuild: true), Is.True);
        }
    }
}
