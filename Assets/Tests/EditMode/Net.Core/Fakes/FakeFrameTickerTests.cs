#nullable enable
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeFrameTickerTests
    {
        [Test]
        public void CadaTickAvisaUmaVez()
        {
            FakeFrameTicker ticker = new FakeFrameTicker();
            int seen = 0;
            ticker.Ticked += () => seen++;

            ticker.Tick();
            ticker.Tick();

            Assert.That(seen, Is.EqualTo(2));
            Assert.That(ticker.Ticks, Is.EqualTo(2));
        }

        [Test]
        public void TickSemAssinanteNaoLanca()
        {
            FakeFrameTicker ticker = new FakeFrameTicker();

            Assert.DoesNotThrow(ticker.Tick);
        }
    }
}
