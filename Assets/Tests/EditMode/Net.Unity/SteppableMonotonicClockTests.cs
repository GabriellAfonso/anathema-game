#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>research R8 da 005: o salto soma ao relógio de dentro e nunca faz o relógio voltar.</summary>
    public class SteppableMonotonicClockTests
    {
        private FakeMonotonicClock inner = null!;
        private SteppableMonotonicClock clock = null!;

        [SetUp]
        public void CreateClock()
        {
            inner = new FakeMonotonicClock();
            clock = new SteppableMonotonicClock(inner);
        }

        [Test]
        public void SemSaltoEhORelogioDeDentro()
        {
            inner.Advance(TimeSpan.FromSeconds(3));

            Assert.That(clock.Now, Is.EqualTo(inner.Now));
        }

        [Test]
        public void SaltosSomamEAcumulam()
        {
            clock.Jump(TimeSpan.FromMinutes(5));
            clock.Jump(TimeSpan.FromSeconds(30));

            Assert.That(clock.Now - inner.Now, Is.EqualTo(TimeSpan.FromSeconds(330)));
        }

        [Test]
        public void DepoisDoSaltoContinuaAndandoComODeDentro()
        {
            clock.Jump(TimeSpan.FromMinutes(1));
            var before = clock.Now;

            inner.Advance(TimeSpan.FromSeconds(2));

            Assert.That(clock.Now - before, Is.EqualTo(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void SaltoNegativoLancaEOrelogioDeDentroNuloTambem()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Jump(TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentNullException>(() => new SteppableMonotonicClock(null!));
        }
    }
}
