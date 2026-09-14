#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class FakeAccessTokenSourceTests
    {
        private FakeMonotonicClock clock = null!;
        private FakeAccessTokenSource tokens = null!;

        [SetUp]
        public void CreateSource()
        {
            clock = new FakeMonotonicClock();
            tokens = new FakeAccessTokenSource(clock);
        }

        [Test]
        public async Task ValidosSaemNaOrdemEOUltimoSeRepete()
        {
            tokens.EnqueueValid("A");
            tokens.EnqueueValid("B");

            Assert.That(await RevealAsync(), Is.EqualTo("A"));
            Assert.That(await RevealAsync(), Is.EqualTo("B"));
            Assert.That(await RevealAsync(), Is.EqualTo("B"));
            Assert.That(tokens.ValidRequests, Is.EqualTo(3));
        }

        [Test]
        public void PedidoSemRoteiroNemTokenLancaDizendoOQueRoteirizar()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => tokens.GetValidAsync());

            Assert.That(error.Message, Does.Contain("EnqueueValid"));
        }

        [Test]
        public async Task RenovadoViraOTokenAtual()
        {
            tokens.EnqueueValid("A");
            await tokens.GetValidAsync();
            tokens.EnqueueRenewed("B");

            RenewalOutcome renewed = await tokens.RenewNowAsync();

            Assert.That(renewed.Token!.RevealForRequest(), Is.EqualTo("B"));
            Assert.That(await RevealAsync(), Is.EqualTo("B"));
            Assert.That(tokens.RenewRequests, Is.EqualTo(1));
        }

        [Test]
        public void RenovacaoSemRoteiroLanca()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => tokens.RenewNowAsync());

            Assert.That(error.Message, Does.Contain("EnqueueRenewed"));
        }

        [Test]
        public async Task SessaoExpiradaNaRenovacaoValeParaOsPedidosSeguintes()
        {
            tokens.EnqueueValid("A");
            await tokens.GetValidAsync();
            tokens.EnqueueRenewalExpired();

            await tokens.RenewNowAsync();
            AccessTokenOutcome after = await tokens.GetValidAsync();

            Assert.That(after.Kind, Is.EqualTo(AccessTokenOutcomeKind.SessionUnavailable));
            Assert.That(after.Session, Is.EqualTo(SessionUnavailableKind.Expired));
        }

        [TestCase(RenewalUnavailableReason.Transport)]
        [TestCase(RenewalUnavailableReason.ServerStatus)]
        [TestCase(RenewalUnavailableReason.OutOfContract)]
        public async Task IndisponivelCarregaOMotivo(RenewalUnavailableReason reason)
        {
            tokens.EnqueueUnavailable(reason);
            tokens.EnqueueRenewalUnavailable(reason);

            AccessTokenOutcome token = await tokens.GetValidAsync();
            RenewalOutcome renewal = await tokens.RenewNowAsync();

            Assert.That(token.Kind, Is.EqualTo(AccessTokenOutcomeKind.Unavailable));
            Assert.That(token.Renewal!.Reason, Is.EqualTo(reason));
            Assert.That(renewal.Reason, Is.EqualTo(reason));
        }

        [Test]
        public async Task RenovacaoSeguraSoCompletaNoRelease()
        {
            tokens.EnqueueRenewed("B");
            HeldRenewal held = tokens.HoldNextRenewal();

            Task<RenewalOutcome> pending = tokens.RenewNowAsync();

            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(held.IsPending, Is.True);
            held.Release();
            Assert.That((await pending).Kind, Is.EqualTo(RenewalOutcomeKind.Renewed));
            Assert.That(held.IsPending, Is.False);
        }

        [Test]
        public void ReleaseAntesDoPedidoLanca()
        {
            HeldRenewal held = tokens.HoldNextRenewal();

            Assert.Throws<InvalidOperationException>(held.Release);
        }

        private async Task<string> RevealAsync()
        {
            AccessTokenOutcome outcome = await tokens.GetValidAsync();
            return outcome.Token!.RevealForRequest();
        }
    }
}
