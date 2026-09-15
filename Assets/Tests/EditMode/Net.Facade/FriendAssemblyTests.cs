#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>
    /// FR-021, FR-022: apresentação, borda de cena e prova nunca são amigas de nenhuma assembly, então só compilam com
    /// o público. A assembly de testes da prova só é amiga de núcleo, JSON, conta e partida (plan.md, Complexity Tracking).
    /// </summary>
    public class FriendAssemblyTests
    {
        private static readonly string[] ProjectAssemblies =
        {
            "Anathema.Net.Core", "Anathema.Net.Json", "Anathema.Net.Account", "Anathema.Net.Connection", "Anathema.Net.Match", "Anathema.Net.Facade",
            "Anathema.Net.Unity", "Anathema.Config", "Anathema.Client.Scenes", "Anathema.Net.Fakes",
        };

        private static readonly string[] NeverFriends = { "Anathema.Presentation", "Anathema.Client.Scenes", "Anathema.Client.Proof" };

        private static readonly string[] ProofTestsMayRead = { "Anathema.Net.Core", "Anathema.Net.Json", "Anathema.Net.Account", "Anathema.Net.Match", "Anathema.Net.Fakes" };

        [TestCaseSource(nameof(ProjectAssemblies))]
        public void ApresentacaoCenasEProvaNaoSaoAmigas(string assemblyName)
        {
            string[] friends = FriendsOf(assemblyName);

            Assert.That(friends.Intersect(NeverFriends), Is.Empty, $"{assemblyName} abre internos para quem só pode ver o público");
        }

        [TestCaseSource(nameof(ProjectAssemblies))]
        public void TestesDaProvaSoLeemNucleoJsonContaEPartida(string assemblyName)
        {
            bool friendOfProofTests = FriendsOf(assemblyName).Contains("Anathema.Client.Proof.Tests");

            Assert.That(friendOfProofTests && !ProofTestsMayRead.Contains(assemblyName), Is.False, $"{assemblyName} abre internos para os testes da prova");
        }

        private static string[] FriendsOf(string assemblyName)
        {
            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));
            return assembly.GetCustomAttributes<InternalsVisibleToAttribute>().Select(attribute => attribute.AssemblyName.Split(',')[0].Trim()).ToArray();
        }
    }
}
