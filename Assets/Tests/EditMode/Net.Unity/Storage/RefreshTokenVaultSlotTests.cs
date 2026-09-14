#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class RefreshTokenVaultSlotTests
    {
        [Test]
        public void JogoInstaladoUsaOSlotPlayer()
        {
            Assert.That(RefreshTokenVaultSlot.ForPlayer().Name, Is.EqualTo("player"));
        }

        [Test]
        public void MesmoProjetoDaSempreOMesmoSlot()
        {
            RefreshTokenVaultSlot first = RefreshTokenVaultSlot.ForEditorProject("C:/Projects/game/Assets");
            RefreshTokenVaultSlot again = RefreshTokenVaultSlot.ForEditorProject("C:/Projects/game/Assets");

            Assert.That(again.Name, Is.EqualTo(first.Name));
            Assert.That(first.Name, Does.Match("^editor-[0-9a-f]{12}$"));
        }

        [Test]
        public void ClonesDoMultiplayerPlayModeTemSlotsDiferentes()
        {
            RefreshTokenVaultSlot main = RefreshTokenVaultSlot.ForEditorProject("C:/Projects/game/Assets");
            RefreshTokenVaultSlot clone = RefreshTokenVaultSlot.ForEditorProject("C:/Projects/game/Library/VP/mppm1a2b/Assets");

            Assert.That(clone.Name, Is.Not.EqualTo(main.Name));
        }

        [TestCase("")]
        [TestCase("Live Test")]
        [TestCase("../escape")]
        public void NomeForaDoPadraoLancaComOValor(string name)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => RefreshTokenVaultSlot.Named(name));

            Assert.That(error.Message, Does.Contain("'" + name + "'"));
        }
    }
}
