#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class RefreshTokenTests
    {
        [TestCase("")]
        [TestCase("   ")]
        public void TextoVazioOuSoComEspacosLanca(string text)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new RefreshToken(text));

            Assert.That(error.Message, Does.Contain("expected"));
        }

        [Test]
        public void ToStringMascaraOTexto()
        {
            RefreshToken refresh = new RefreshToken("eyJ.refresh-secret.sig");

            Assert.That(refresh.ToString(), Is.EqualTo("refresh_token=<redacted>"));
            Assert.That(refresh.ToString(), Does.Not.Contain("refresh-secret"));
        }

        [Test]
        public void RevealForRequestDevolveOTexto()
        {
            Assert.That(new RefreshToken("eyJ.refresh.sig").RevealForRequest(), Is.EqualTo("eyJ.refresh.sig"));
        }

        [Test]
        public void IgualdadePorValor()
        {
            Assert.That(new RefreshToken("a.b.c").Equals(new RefreshToken("a.b.c")), Is.True);
            Assert.That(new RefreshToken("a.b.c").Equals(new RefreshToken("a.b.d")), Is.False);
        }
    }
}
