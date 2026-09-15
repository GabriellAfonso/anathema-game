#nullable enable
using System.Linq;
using System.Reflection;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>FR-020, research R3: nenhuma assinatura da superfície é event do C#; todas devolvem algo descartável.</summary>
    public class SurfaceHasNoEventTests
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Test]
        public void NenhumTipoPublicoDaSuperficieDeclaraEvent()
        {
            Assembly[] surface = { typeof(UserId).Assembly, typeof(LoadedCatalog).Assembly, typeof(GiveUpReason).Assembly, typeof(LiveMatch).Assembly, typeof(AnathemaClient).Assembly };

            string[] events = surface.SelectMany(assembly => assembly.GetExportedTypes())
                .SelectMany(type => type.GetEvents(Declared).Select(declared => type.FullName + "." + declared.Name))
                .ToArray();

            Assert.That(events, Is.Empty);
        }
    }
}
