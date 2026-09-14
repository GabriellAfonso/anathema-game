#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Anathema.Net.Account;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-030, SC-008: comandos e pendente não guardam nada de onde uma regra local poderia sair.</summary>
    public class CommandsIgnoreHintsTests
    {
        private static readonly Type[] Forbidden =
        {
            typeof(MatchMirror), typeof(LoadedCatalog), typeof(HandCardHint), typeof(DisplayedUnitStats), typeof(TurnClock), typeof(PlayerView),
        };

        [TestCase(typeof(MatchCommands))]
        [TestCase(typeof(PendingPlay))]
        public void NaoTemCampoDeEspelhoCatalogoOuDica(Type type)
        {
            string[] offenders = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => Forbidden.Contains(field.FieldType))
                .Select(field => field.Name)
                .ToArray();

            Assert.That(offenders, Is.Empty);
        }
    }
}
