#nullable enable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// <c>PlayerPrefs</c> é texto puro e não guarda credencial (constituição; FR-022). O jogo não
    /// tem outro uso legítimo dele hoje, então nenhum script chama a API; credencial vai para
    /// <c>IRefreshTokenVault</c>. Menção em comentário não conta, só chamada.
    /// </summary>
    public class NoCredentialInPlayerPrefsTests
    {
        private static readonly Regex PlayerPrefsCall = new Regex(@"\bPlayerPrefs\s*\.");

        [Test]
        public void NenhumScriptDoJogoChamaPlayerPrefs()
        {
            string scripts = Path.Combine(Application.dataPath, "Scripts");

            string[] offenders = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .Where(path => PlayerPrefsCall.IsMatch(File.ReadAllText(path)))
                .Select(path => path.Substring(scripts.Length + 1))
                .ToArray();

            Assert.That(offenders, Is.Empty, "credentials go to IRefreshTokenVault (Android Keystore, DPAPI), never PlayerPrefs");
        }
    }
}
