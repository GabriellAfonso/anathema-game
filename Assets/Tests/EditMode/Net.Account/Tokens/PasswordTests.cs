#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class PasswordTests
    {
        [Test]
        public void SenhaVaziaLanca()
        {
            Assert.Throws<ArgumentException>(() => new Password(""));
        }

        [Test]
        public void ToStringMascara()
        {
            Password password = new Password("hunter2-secret");

            Assert.That(password.ToString(), Is.EqualTo("password=<redacted>"));
        }

        [Test]
        public void RevealForRequestDevolveOTexto()
        {
            Assert.That(new Password("123456").RevealForRequest(), Is.EqualTo("123456"));
        }
    }
}
