#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class UnityFrameTickerTests
    {
        [Test]
        public void CadaRaiseAvisaUmaVez()
        {
            UnityFrameTicker ticker = new UnityFrameTicker();
            int ticks = 0;
            ticker.Ticked += () => ticks++;

            ticker.Raise();
            ticker.Raise();

            Assert.That(ticks, Is.EqualTo(2));
        }

        [Test]
        public void RaiseSemAssinanteNaoLanca()
        {
            Assert.DoesNotThrow(new UnityFrameTicker().Raise);
        }
    }
}
