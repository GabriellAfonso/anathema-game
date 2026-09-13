#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class MonotonicInstantTests
    {
        [Test]
        public void SubtracaoDevolveADuracaoExataEmTicks()
        {
            MonotonicInstant earlier = new MonotonicInstant(400);
            MonotonicInstant later = new MonotonicInstant(1000);

            Assert.That(later - earlier, Is.EqualTo(TimeSpan.FromTicks(600)));
        }

        [Test]
        public void AddDeslocaPelaDuracao()
        {
            MonotonicInstant start = new MonotonicInstant(10);

            Assert.That(start.Add(TimeSpan.FromTicks(5)), Is.EqualTo(new MonotonicInstant(15)));
        }

        [Test]
        public void InstantesComOsMesmosTicksSaoIguais()
        {
            Assert.That(new MonotonicInstant(7) == new MonotonicInstant(7), Is.True);
            Assert.That(new MonotonicInstant(7).GetHashCode(), Is.EqualTo(new MonotonicInstant(7).GetHashCode()));
        }

        [Test]
        public void ComparacaoSegueOsTicks()
        {
            MonotonicInstant first = new MonotonicInstant(1);
            MonotonicInstant sameAsFirst = new MonotonicInstant(1);
            MonotonicInstant second = new MonotonicInstant(2);

            Assert.That(first < second && second > first && first <= sameAsFirst && second >= first, Is.True);
            Assert.That(first.CompareTo(second), Is.Negative);
        }

        [Test]
        public void NaoExpoeHoraAbsoluta()
        {
            MethodInfo[] methods = typeof(MonotonicInstant).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            bool usesDateTime = methods.Any(method =>
                method.ReturnType == typeof(DateTime) ||
                method.GetParameters().Any(parameter => parameter.ParameterType == typeof(DateTime)));

            Assert.That(usesDateTime, Is.False);
        }
    }
}
