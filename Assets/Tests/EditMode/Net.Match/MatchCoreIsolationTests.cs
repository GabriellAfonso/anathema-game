#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Anathema.Net.Connection;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>Research R1: espelho, relógio, dicas, leitores e formas não recebem nada da conexão.</summary>
    public class MatchCoreIsolationTests
    {
        private static readonly Assembly ConnectionAssembly = typeof(AuthenticatedConnection).Assembly;

        [TestCase(typeof(MatchMirror))]
        [TestCase(typeof(TurnClock))]
        [TestCase(typeof(VersionGate))]
        [TestCase(typeof(DisplayedUnitStats))]
        [TestCase(typeof(HandCardHint))]
        [TestCase(typeof(PendingPlay))]
        [TestCase(typeof(PlayerView))]
        [TestCase(typeof(MatchStartFrame))]
        [TestCase(typeof(MatchUpdateFrame))]
        [TestCase(typeof(TurnWarningFrame))]
        [TestCase(typeof(ClockView))]
        [TestCase(typeof(TurnView))]
        [TestCase(typeof(BankUnit))]
        [TestCase(typeof(MatchCard))]
        [TestCase(typeof(PlayRefusal))]
        public void NaoRecebeNemGuardaTipoDaConexao(Type type)
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            Type[] used = type.GetConstructors(all).SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType)
                .Concat(type.GetFields(all).Select(field => field.FieldType))
                .ToArray();

            Assert.That(used.Where(usedType => usedType.Assembly == ConnectionAssembly).Select(usedType => usedType.Name), Is.Empty);
        }
    }
}
