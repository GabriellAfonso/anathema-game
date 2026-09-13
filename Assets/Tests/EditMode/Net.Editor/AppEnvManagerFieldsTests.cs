#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;

namespace Anathema.Net.Editor.Tests
{
    /// <summary>
    /// O gate lê o AppEnvManager por nome de campo (plan.md, Complexity Tracking). Se alguém
    /// renomear um campo, este teste falha antes de o gate passar a ignorar a cena em silêncio.
    /// </summary>
    public class AppEnvManagerFieldsTests
    {
        [TestCase(EnvironmentSelectionReader.IsProdField)]
        [TestCase(EnvironmentSelectionReader.DevConfigField)]
        [TestCase(EnvironmentSelectionReader.ProdConfigField)]
        public void CampoLidoPeloGateExisteNoAppEnvManager(string field)
        {
            Type? type = EditorTestObjects.AppEnvManagerType();
            Assert.That(type, Is.Not.Null, "AppEnvManager script not found");

            Assert.That(type!.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Is.Not.Null, $"{type.Name}.{field} is gone");
        }
    }
}
