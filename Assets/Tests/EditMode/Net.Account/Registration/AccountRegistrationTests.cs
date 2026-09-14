#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountRegistrationTests
    {
        private const string PasswordText = "registration-password-secret";

        [Test]
        public async Task CorpoTemExatamenteOsQuatroCampos()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(201, "{\"detail\": \"User registered successfully\"}");

            await Register(rig);

            IPayloadReader body = AccountTestCodec.Reader(rig.Http.Requests[0].Body!);
            Assert.That(body.FieldNames, Is.EquivalentTo(new[] { "username", "email", "password", "password_confirmation" }));
            Assert.That(rig.Http.Requests[0].Url, Is.EqualTo(rig.Routes.Register));
            Assert.That(rig.Http.Requests[0].Headers.ContainsKey("Authorization"), Is.False);
        }

        [Test]
        public async Task CadastroAceitoNaoAutentica()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(201, "{\"detail\": \"User registered successfully\"}");

            AccountCallOutcome<AccountCreated, RegistrationRefusal> outcome = await Register(rig);

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(rig.Vault.SaveCount, Is.EqualTo(0));
        }

        [Test]
        public async Task EmailJaUsadoEhRecusaNoCampoEmail()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(400, "{\"email\": [\"Este e-mail já está sendo utilizado.\"]}");

            AccountCallOutcome<AccountCreated, RegistrationRefusal> outcome = await Register(rig);

            RegistrationFieldError error = outcome.Refusal!.Fields.Single();
            Assert.That(error.Field, Is.EqualTo(RegistrationField.Email));
            Assert.That(error.Messages, Is.EqualTo(new[] { "Este e-mail já está sendo utilizado." }));
        }

        [Test]
        public async Task VariosCamposVemNaOrdemDoCorpoComDesconhecidoPreservado()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(400, "{\"password\": [\"too short\", \"too common\"], \"password_confirmation\": [\"Passwords must match.\"], \"non_field_errors\": [\"try again\"]}");

            AccountCallOutcome<AccountCreated, RegistrationRefusal> outcome = await Register(rig);

            RegistrationFieldError[] fields = outcome.Refusal!.Fields.ToArray();
            Assert.That(fields.Select(field => field.Field), Is.EqualTo(new[] { RegistrationField.Password, RegistrationField.PasswordConfirmation, RegistrationField.Other }));
            Assert.That(fields[0].Messages.Count, Is.EqualTo(2));
            Assert.That(fields[2].FieldName, Is.EqualTo("non_field_errors"));
        }

        [TestCase(400, "[\"not a dictionary\"]")]
        [TestCase(400, "{}")]
        [TestCase(500, "<html>boom</html>")]
        public async Task RespostaSemFormaDeCamposNaoEhReconhecida(int status, string body)
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(status, body);

            AccountCallOutcome<AccountCreated, RegistrationRefusal> outcome = await Register(rig);

            Assert.That(outcome.Refusal!.Fields, Is.Empty);
            Assert.That(outcome.Refusal.Unrecognized!.Status, Is.EqualTo(status));
        }

        [Test]
        public async Task FalhaDeTransporteEhFalhaComum()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.FailNext(TransportFailureKind.Timeout, "Request timeout");

            AccountCallOutcome<AccountCreated, RegistrationRefusal> outcome = await Register(rig);

            Assert.That(outcome.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.TransportFailed));
        }

        [Test]
        public async Task SenhaNuncaVaiParaOLog()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(400, "{\"password\": [\"too short\"]}");

            await Register(rig);

            Assert.That(rig.Log.Entries.SelectMany(entry => entry.Fields).Select(field => field.Value), Has.None.Contains(PasswordText));
        }

        private static Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>> Register(AccountTestRig rig)
        {
            RegistrationForm form = new RegistrationForm("one", "one@example.com", new Password(PasswordText), new Password(PasswordText));
            return new AccountRegistration(rig.Http, rig.Codec, rig.Log, rig.Routes).RegisterAsync(form);
        }
    }
}
