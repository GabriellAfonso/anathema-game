#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US2-1, US2-2, US2-3, US2-6, FR-002, FR-008: a conta pela fachada.</summary>
    public class ClientAccountTests
    {
        private const string ProfileJson = "{\"nickname\": \"one\", \"icon\": \"default_icon\", \"level\": 3, \"experience_points\": 120, \"coins\": 50, \"credits\": 2}";

        private FacadeTestRig rig = null!;
        private List<ClientStageChange> changes = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            changes = new List<ClientStageChange>();
            rig.Client.StageChanged.Subscribe(changes.Add);
        }

        [Test]
        public async Task CadastroNaoEntraNemMudaOEstagio()
        {
            rig.Http.RespondNext(201, "{\"detail\": \"User registered successfully\"}");
            RegistrationForm form = new RegistrationForm("one", "one@live.local", new Password("123456"), new Password("123456"));

            AccountCallOutcome<AccountCreated, RegistrationRefusal> created = await rig.Client.Account.RegisterAsync(form);
            rig.Pump();

            Assert.That(created.IsSuccess, Is.True);
            Assert.That((rig.Client.State.Stage, changes.Count), Is.EqualTo((ClientStage.SignedOut, 0)));
        }

        [Test]
        public async Task LoginRecusadoMantemDeslogado()
        {
            rig.Http.RespondNext(401, "{\"detail\": \"No active account found with the given credentials\"}");

            SignInOutcome outcome = await rig.Client.Account.SignInAsync("one", new Password("errada"));
            rig.Pump();

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.CredentialsRefused));
            Assert.That((rig.Client.State.Stage, changes.Count), Is.EqualTo((ClientStage.SignedOut, 0)));
        }

        [Test]
        public async Task EntrarJaLogadoDevolveJaLogadoSemEnviar()
        {
            await rig.SignInAsync();
            int requests = rig.Http.Requests.Count;

            SignInOutcome outcome = await rig.Client.Account.SignInAsync("two", new Password("123456"));

            Assert.That((outcome.Kind, outcome.User), Is.EqualTo((SignInOutcomeKind.AlreadySignedIn, (UserId?)new UserId(7))));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(requests));
        }

        [Test]
        public async Task PerfilDevolveOResultadoDaConta()
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(200, ProfileJson);

            AccountCallOutcome<OwnProfile, ProfileRefusal> profile = await rig.Client.Account.ReadProfileAsync();

            Assert.That((profile.Value.Nickname, profile.Value.Coins, profile.Value.Level), Is.EqualTo(("one", 50L, 3L)));
            Assert.That(rig.Client.Account.Self, Is.EqualTo(new UserId(7)));
        }

        [Test]
        public async Task OutraFachadaSobreAMesmaGuardaRetomaComOMesmoUsuario()
        {
            await rig.SignInAsync();
            rig.Client.Dispose();
            FacadeTestRig reopened = new FacadeTestRig(rig.Vault);
            reopened.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("resumed")));

            ResumeOutcome resumed = await reopened.Client.Account.ResumeAsync();
            reopened.Pump();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed));
            Assert.That((reopened.Client.State.Stage, reopened.Client.State.Self), Is.EqualTo((ClientStage.SignedIn, (UserId?)new UserId(7))));
        }
    }
}
