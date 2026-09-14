#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class SpoiledFirstTokenSourceTests
    {
        [Test]
        public async Task SoOPrimeiroTokenValidoSaiEstragado()
        {
            FakeAccessTokenSource inner = new FakeAccessTokenSource(new FakeMonotonicClock());
            inner.EnqueueValid("token-A");
            inner.EnqueueRenewed("token-B");
            SpoiledFirstTokenSource tokens = new SpoiledFirstTokenSource(inner);

            AccessTokenOutcome first = await tokens.GetValidAsync();
            AccessTokenOutcome second = await tokens.GetValidAsync();
            RenewalOutcome renewed = await tokens.RenewNowAsync();

            Assert.That(first.Token!.RevealForRequest(), Is.Not.EqualTo("token-A").And.Contain("token-A"));
            Assert.That(first.Token.Owner, Is.EqualTo(second.Token!.Owner));
            Assert.That(second.Token.RevealForRequest(), Is.EqualTo("token-A"));
            Assert.That(renewed.Token!.RevealForRequest(), Is.EqualTo("token-B"));
        }

        [Test]
        public async Task DesfechoQueNaoEhValidoPassaDireto()
        {
            FakeAccessTokenSource inner = new FakeAccessTokenSource(new FakeMonotonicClock());
            inner.EnqueueSessionUnavailable(SessionUnavailableKind.NoSession);
            inner.EnqueueValid("token-A");
            SpoiledFirstTokenSource tokens = new SpoiledFirstTokenSource(inner);

            AccessTokenOutcome unavailable = await tokens.GetValidAsync();
            AccessTokenOutcome valid = await tokens.GetValidAsync();

            Assert.That(unavailable.Kind, Is.EqualTo(AccessTokenOutcomeKind.SessionUnavailable));
            Assert.That(valid.Token!.RevealForRequest(), Is.Not.EqualTo("token-A"));
        }
    }
}
