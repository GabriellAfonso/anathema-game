#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    /// <summary>research R4 da 005: login pedido com sessão já aberta devolve um valor próprio, sem envio.</summary>
    public class SignInOutcomeTests
    {
        [Test]
        public void JaLogadoTemODonoDaSessaoEForma()
        {
            SignInOutcome outcome = SignInOutcome.AlreadySignedIn(new UserId(7));

            Assert.That((outcome.Kind, outcome.User), Is.EqualTo((SignInOutcomeKind.AlreadySignedIn, (UserId?)new UserId(7))));
            Assert.That(outcome.ToString(), Does.StartWith("AlreadySignedIn").And.Contain("7"));
        }
    }
}
