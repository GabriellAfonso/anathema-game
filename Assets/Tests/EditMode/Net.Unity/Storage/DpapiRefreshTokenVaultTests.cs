#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.IO;
using System.Text;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// A guarda DPAPI de verdade, numa pasta temporária apagada no fim. É o teste do próprio
    /// adaptador; quem consome a guarda usa o FakeRefreshTokenVault.
    /// </summary>
    public class DpapiRefreshTokenVaultTests
    {
        private const string Token = "eyJ.refresh-token-secret.sig";
        private string directory = string.Empty;

        [SetUp]
        public void CreateDirectory()
        {
            directory = Path.Combine(Path.GetTempPath(), "anathema-vault-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void DeleteDirectory()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void GravarELerDevolveOMesmoToken()
        {
            DpapiRefreshTokenVault vault = Vault("slot-a");

            Assert.That(vault.Save(new RefreshToken(Token)).Kind, Is.EqualTo(VaultWriteKind.Saved));

            Assert.That(vault.Read().Token!.RevealForRequest(), Is.EqualTo(Token));
        }

        [Test]
        public void OutraInstanciaNoMesmoSlotLeOToken()
        {
            Vault("slot-a").Save(new RefreshToken(Token));

            Assert.That(Vault("slot-a").Read().Kind, Is.EqualTo(VaultReadKind.Found));
        }

        [Test]
        public void SlotDiferenteNaoEnxergaOToken()
        {
            Vault("slot-a").Save(new RefreshToken(Token));

            Assert.That(Vault("slot-b").Read().Kind, Is.EqualTo(VaultReadKind.Empty));
        }

        [Test]
        public void ArquivoCorrompidoEhIlegivelSemExcecao()
        {
            Vault("slot-a").Save(new RefreshToken(Token));
            File.WriteAllBytes(FileOf("slot-a"), new byte[] { 1, 2, 3, 4 });

            VaultReadOutcome read = Vault("slot-a").Read();

            Assert.That(read.Kind, Is.EqualTo(VaultReadKind.Unreadable));
            Assert.That(read.Detail, Does.StartWith("dpapi_error="));
        }

        [Test]
        public void ArquivoNoDiscoNaoContemOTexto()
        {
            Vault("slot-a").Save(new RefreshToken(Token));

            byte[] stored = File.ReadAllBytes(FileOf("slot-a"));

            Assert.That(Encoding.UTF8.GetString(stored), Does.Not.Contain("refresh-token-secret"));
            Assert.That(Encoding.Unicode.GetString(stored), Does.Not.Contain("refresh-token-secret"));
        }

        [Test]
        public void ApagarDuasVezesDeixaVazio()
        {
            DpapiRefreshTokenVault vault = Vault("slot-a");
            vault.Save(new RefreshToken(Token));

            vault.Delete();
            vault.Delete();

            Assert.That(vault.Read().Kind, Is.EqualTo(VaultReadKind.Empty));
        }

        private DpapiRefreshTokenVault Vault(string slot) => new DpapiRefreshTokenVault(directory, RefreshTokenVaultSlot.Named(slot));

        private string FileOf(string slot) => Path.Combine(directory, $"refresh_token.{slot}.bin");
    }
}
#endif
