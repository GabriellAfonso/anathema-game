#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class MonotonicClockAdapterTests
    {
        [Test]
        public void StopwatchNuncaDiminui()
        {
            StopwatchMonotonicClock clock = new StopwatchMonotonicClock();
            MonotonicInstant previous = clock.Now;

            for (int reading = 0; reading < 10000; reading++)
            {
                MonotonicInstant current = clock.Now;
                Assert.That(current >= previous, Is.True, $"{current} came before {previous}");
                previous = current;
            }
        }

        [Test]
        public void NoEditorAPlataformaUsaStopwatch()
        {
            Assert.That(PlatformMonotonicClock.Create(new FakeClientLog()), Is.InstanceOf<StopwatchMonotonicClock>());
        }
    }
}
