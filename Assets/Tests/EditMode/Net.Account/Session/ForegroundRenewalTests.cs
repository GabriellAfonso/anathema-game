#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class ForegroundRenewalTests
    {
        [Test]
        public async Task VoltarComTokenVencidoRenovaAntesDeAlguemPedir()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            using ForegroundRenewal onReturn = Attach(rig);
            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(7));
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
            rig.Log.Single("access_token_renewal_on_foreground");
        }

        [Test]
        public async Task VoltarDepoisDeUmMinutoNaoRenova()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            using ForegroundRenewal onReturn = Attach(rig);
            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(1));

            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(0));
        }

        [Test]
        public void SemSessaoNaoRenova()
        {
            AccountTestRig rig = new AccountTestRig();
            using ForegroundRenewal onReturn = Attach(rig);
            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(7));

            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.Http.Requests, Is.Empty);
        }

        [Test]
        public async Task DepoisDeDisposeNaoRenova()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            Attach(rig).Dispose();
            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(7));

            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(0));
        }

        private static ForegroundRenewal Attach(AccountTestRig rig)
        {
            return new ForegroundRenewal(rig.Lifecycle, rig.Session, rig.Tokens, rig.Clock, rig.Timing, rig.Log);
        }
    }
}
