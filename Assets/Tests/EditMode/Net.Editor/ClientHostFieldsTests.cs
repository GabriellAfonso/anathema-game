#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;

namespace Anathema.Net.Editor.Tests
{
    /// <summary>
    /// O gate lê o AppEnvManager por nome de campo (plan.md, Complexity Tracking). Se alguém
    /// renomear um campo, este teste falha antes de o gate passar a ignorar a cena em silêncio.
    /// Desde a 005 os campos moram no <c>ClientHost</c> (specs/005-presentation-facade/research.md, R9).
    /// </summary>
    public class ClientHostFieldsTests
    {
        [TestCase(EnvironmentSelectionReader.IsProdField)]
        [TestCase(EnvironmentSelectionReader.DevConfigField)]
        [TestCase(EnvironmentSelectionReader.ProdConfigField)]
        public void CampoLidoPeloGateExisteNoClientHost(string field)
        {
            Type? type = EditorTestObjects.SelectorType();
            Assert.That(type, Is.Not.Null, "ClientHost script not found");

            Assert.That(type!.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Is.Not.Null, $"{type.Name}.{field} is gone");
        }
    }
}
