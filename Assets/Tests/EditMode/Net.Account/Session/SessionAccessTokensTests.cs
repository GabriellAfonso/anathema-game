#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class SessionAccessTokensTests
    {
        [Test]
        public async Task SemLoginNaoHaSessaoENemPedido()
        {
            AccountTestRig rig = new AccountTestRig();

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Kind, Is.EqualTo(AccessTokenOutcomeKind.SessionUnavailable));
            Assert.That(token.Session, Is.EqualTo(SessionUnavailableKind.NoSession));
            Assert.That(rig.Http.Requests, Is.Empty);
        }

        [Test]
        public async Task DepoisDeExpirarDizExpirada()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(401, "{}");
            await rig.Tokens.RenewNowAsync();

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Session, Is.EqualTo(SessionUnavailableKind.Expired));
        }

        [Test]
        public async Task ForaDaMargemDevolveOAtualSemPedido()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(60));

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Token!.RevealForRequest(), Is.EqualTo(FakeAccessJwt.FiveMinutes("login")));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task UmSegundoAntesDaMargemNaoRenova()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(269));

            await rig.Tokens.GetValidAsync();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(0));
        }

        [Test]
        public async Task NaMargemRenovaAntesDeEntregar()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(271));
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Kind, Is.EqualTo(AccessTokenOutcomeKind.Valid));
            Assert.That(token.Token!.RevealForRequest(), Is.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
        }

        [Test]
        public async Task RenovarAgoraIgnoraAMargem()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(10));
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            RenewalOutcome renewed = await rig.Tokens.RenewNowAsync();

            Assert.That(renewed.Kind, Is.EqualTo(RenewalOutcomeKind.Renewed));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
        }

        [Test]
        public async Task DezPedidosConcorrentesNaMargemGeramUmaRenovacao()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(280));
            HeldHttpResponse held = rig.Http.HoldNext();

            Task<AccessTokenOutcome>[] waiting = Enumerable.Range(0, 10).Select(_ => rig.Tokens.GetValidAsync()).ToArray();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
            held.Release(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            AccessTokenOutcome[] tokens = await Task.WhenAll(waiting);
            Assert.That(tokens.Select(token => token.Token!.RevealForRequest()), Is.All.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
        }

        [Test]
        public async Task RenovacaoQueNaoSaiViraIndisponivel()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Clock.Advance(TimeSpan.FromSeconds(280));
            rig.Http.FailNext(TransportFailureKind.HostNotResolved, "Cannot resolve destination host");

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Kind, Is.EqualTo(AccessTokenOutcomeKind.Unavailable));
            Assert.That(token.Renewal!.Reason, Is.EqualTo(RenewalUnavailableReason.Transport));
        }

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
