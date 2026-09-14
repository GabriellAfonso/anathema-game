#nullable enable
using System.Threading.Tasks;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class OwnProfileQueryTests
    {
        private const string ProfileJson = "{\"nickname\": \"one\", \"icon\": \"default_icon\", \"level\": 3, \"experience_points\": 120, \"coins\": 50, \"credits\": 2}";

        [Test]
        public async Task PerfilCompletoTemOsSeisCampos()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, ProfileJson);

            AccountCallOutcome<OwnProfile, ProfileRefusal> outcome = await Query(rig).ReadAsync();

            OwnProfile profile = outcome.Value;
            Assert.That(profile.Nickname, Is.EqualTo("one"));
            Assert.That(profile.Icon, Is.EqualTo("default_icon"));
            Assert.That(profile.Level, Is.EqualTo(3));
            Assert.That(profile.ExperiencePoints, Is.EqualTo(120));
            Assert.That(profile.Coins, Is.EqualTo(50));
            Assert.That(profile.Credits, Is.EqualTo(2));
            Assert.That(rig.Http.Requests[1].Url, Is.EqualTo(rig.Routes.OwnProfile));
        }

        [Test]
        public async Task ContaSemPerfilEhRecusaExplicita()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");

            AccountCallOutcome<OwnProfile, ProfileRefusal> outcome = await Query(rig).ReadAsync();

            Assert.That(outcome.Refusal!.Kind, Is.EqualTo(ProfileRefusalKind.ProfileMissing));
        }

        [Test]
        public async Task ErroDoServidorEhRecusaNaoReconhecida()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(500, "<html>boom</html>");

            AccountCallOutcome<OwnProfile, ProfileRefusal> outcome = await Query(rig).ReadAsync();

            Assert.That(outcome.Refusal!.Kind, Is.EqualTo(ProfileRefusalKind.Unrecognized));
            Assert.That(outcome.Refusal.Unrecognized!.Status, Is.EqualTo(500));
        }

        [Test]
        public async Task NivelEmTextoEhForaDoContrato()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, ProfileJson.Replace("\"level\": 3", "\"level\": \"3\""));

            AccountCallOutcome<OwnProfile, ProfileRefusal> outcome = await Query(rig).ReadAsync();

            Assert.That(outcome.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.OutOfContract));
            Assert.That(outcome.Failure.Decode!.Path, Is.EqualTo("level"));
            rig.Log.Single("account_response_out_of_contract");
        }

        private static OwnProfileQuery Query(AccountTestRig rig) => new OwnProfileQuery(rig.Client, rig.Codec, rig.Log, rig.Routes);

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
