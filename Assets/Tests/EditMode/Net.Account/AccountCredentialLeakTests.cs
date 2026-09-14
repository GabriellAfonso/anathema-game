#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    /// <summary>
    /// Senha, token de acesso e refresh token nunca aparecem em log (FR-040, SC-003), em nenhum
    /// caminho: sucesso, recusa, falha de transporte e resposta fora do contrato. Confere o texto
    /// inteiro e o pedaço de claims do JWT, que é o que uma mensagem de falha ecoaria.
    /// </summary>
    public class AccountCredentialLeakTests
    {
        private static readonly string[] AccessTokens =
        {
            FakeAccessJwt.FiveMinutes("login"), FakeAccessJwt.FiveMinutes("renewed"), FakeAccessJwt.FiveMinutes("resumed"),
        };

        [Test]
        public async Task LoginAceitoRecusadoEForaDoContrato()
        {
            await AssertSilentAsync(rig => rig.SignInAsync());
            await AssertSilentAsync(rig => SignInRespondingAsync(rig, 401, "{\"detail\": \"Invalid credentials\"}"));
            await AssertSilentAsync(rig => SignInRespondingAsync(rig, 200, AccountTestRig.LoginBody("broken.token")));
            await AssertSilentAsync(rig => SignInRespondingAsync(rig, 200, "not json " + AccountTestRig.RefreshText));
        }

        [Test]
        public async Task RenovacaoEmTodosOsDesfechos()
        {
            await AssertSilentAsync(rig => RenewRespondingAsync(rig, 200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed"))));
            await AssertSilentAsync(rig => RenewRespondingAsync(rig, 401, "{}"));
            await AssertSilentAsync(rig => RenewRespondingAsync(rig, 503, "<html>"));
            await AssertSilentAsync(rig => RenewRespondingAsync(rig, 200, "{\"access\": \"broken\"}"));
            await AssertSilentAsync(async rig =>
            {
                await rig.SignInAsync();
                rig.Http.FailNext(TransportFailureKind.Timeout, "Request timeout");
                await rig.Tokens.RenewNowAsync();
            });
        }

        [Test]
        public async Task RepeticaoDepoisDe401()
        {
            await AssertSilentAsync(async rig =>
            {
                await rig.SignInAsync();
                rig.Http.RespondNext(401, "{}");
                rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
                rig.Http.RespondNext(200, "{}");
                await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));
            });
        }

        [Test]
        public async Task RetomadaEmTodosOsDesfechos()
        {
            await AssertSilentAsync(rig => ResumeRespondingAsync(rig, 200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("resumed"))));
            await AssertSilentAsync(rig => ResumeRespondingAsync(rig, 401, "{}"));
            await AssertSilentAsync(rig => ResumeRespondingAsync(rig, 503, "{}"));
            await AssertSilentAsync(async rig =>
            {
                rig.Vault.Preload(AccountTestRig.RefreshText);
                rig.Vault.MakeUnreadable("key_invalidated");
                await rig.Session.ResumeAsync();
            });
        }

        [Test]
        public async Task CadastroRecusadoEPerfilLido()
        {
            await AssertSilentAsync(rig =>
            {
                rig.Http.RespondNext(400, "{\"password\": [\"too short\"]}");
                RegistrationForm form = new RegistrationForm("one", "one@example.com", new Password(AccountTestRig.PasswordText), new Password(AccountTestRig.PasswordText));
                return new AccountRegistration(rig.Http, rig.Codec, rig.Log, rig.Routes).RegisterAsync(form);
            });
            await AssertSilentAsync(async rig =>
            {
                await rig.SignInAsync();
                rig.Http.RespondNext(200, "{\"nickname\": \"one\", \"icon\": \"i\", \"level\": \"wrong\", \"experience_points\": 0, \"coins\": 0, \"credits\": 0}");
                await new OwnProfileQuery(rig.Client, rig.Codec, rig.Log, rig.Routes).ReadAsync();
            });
        }

        [Test]
        public async Task FalhaAoGravarNaGuarda()
        {
            await AssertSilentAsync(rig =>
            {
                rig.Vault.FailNextSave("disk_full");
                return rig.SignInAsync();
            });
        }

        private static Task SignInRespondingAsync(AccountTestRig rig, int status, string body)
        {
            rig.Http.RespondNext(status, body);
            return rig.Session.SignInAsync(AccountTestRig.Username, new Password(AccountTestRig.PasswordText));
        }

        private static async Task RenewRespondingAsync(AccountTestRig rig, int status, string body)
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(status, body);
            await rig.Tokens.RenewNowAsync();
        }

        private static Task ResumeRespondingAsync(AccountTestRig rig, int status, string body)
        {
            rig.Vault.Preload(AccountTestRig.RefreshText);
            rig.Http.RespondNext(status, body);
            return rig.Session.ResumeAsync();
        }

        private static async Task AssertSilentAsync(Func<AccountTestRig, Task> scenario)
        {
            AccountTestRig rig = new AccountTestRig();
            await scenario(rig);

            string[] logged = rig.Log.Entries.SelectMany(entry => entry.Fields.Select(field => field.Value).Append(entry.EventName)).ToArray();
            foreach (string secret in Secrets())
                Assert.That(logged, Has.None.Contains(secret), $"a log entry leaked a credential; events: {string.Join(", ", rig.Log.Entries.Select(entry => entry.EventName))}");
        }

        private static string[] Secrets()
        {
            return AccessTokens.Concat(AccessTokens.Select(token => token.Split('.')[1]))
                .Append(AccountTestRig.PasswordText).Append(AccountTestRig.RefreshText).ToArray();
        }
    }
}
