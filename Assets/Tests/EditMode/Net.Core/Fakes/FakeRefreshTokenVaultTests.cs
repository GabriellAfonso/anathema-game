#nullable enable
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeRefreshTokenVaultTests
    {
        [Test]
        public void GravarELerDevolveOMesmoToken()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();

            vault.Save(new RefreshToken("refresh-1"));

            VaultReadOutcome read = vault.Read();
            Assert.That(read.Kind, Is.EqualTo(VaultReadKind.Found));
            Assert.That(read.Token!.RevealForRequest(), Is.EqualTo("refresh-1"));
            Assert.That(vault.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void ApagarDuasVezesDeixaVazio()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();
            vault.Preload("refresh-1");

            vault.Delete();
            vault.Delete();

            Assert.That(vault.Read().Kind, Is.EqualTo(VaultReadKind.Empty));
            Assert.That(vault.DeleteCount, Is.EqualTo(2));
            Assert.That(vault.Stored, Is.Null);
        }

        [Test]
        public void FalhaRoteirizadaValeSoParaAProximaGravacao()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();
            vault.FailNextSave("disk_full");

            VaultWriteOutcome first = vault.Save(new RefreshToken("refresh-1"));
            VaultWriteOutcome second = vault.Save(new RefreshToken("refresh-1"));

            Assert.That(first.Kind, Is.EqualTo(VaultWriteKind.Failed));
            Assert.That(first.Detail, Is.EqualTo("disk_full"));
            Assert.That(second.Kind, Is.EqualTo(VaultWriteKind.Saved));
            Assert.That(vault.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void IlegivelDevolveUnreadableComOMotivo()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();
            vault.Preload("refresh-1");
            vault.MakeUnreadable("key_invalidated");

            VaultReadOutcome read = vault.Read();

            Assert.That(read.Kind, Is.EqualTo(VaultReadKind.Unreadable));
            Assert.That(read.Detail, Is.EqualTo("key_invalidated"));
            Assert.That(read.Token, Is.Null);
        }

        [Test]
        public void PreloadNaoContaComoGravacao()
        {
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();

            vault.Preload("refresh-1");

            Assert.That(vault.Read().Kind, Is.EqualTo(VaultReadKind.Found));
            Assert.That(vault.SaveCount, Is.EqualTo(0));
        }
    }
}
