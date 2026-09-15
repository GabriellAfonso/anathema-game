#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Cadastra conta em <c>POST /accounts/register/</c> (FR-002, FR-003). A rota é pública e não
    /// passa pelo cliente autenticado. 201 não autentica nem muda a sessão; 400 traz um dicionário
    /// campo → lista de mensagens (<c>RegisterSerializer</c> do backend).
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;AccountCreated, RegistrationRefusal&gt; registered = await registration.RegisterAsync(form);
    /// </code>
    /// </example>
    internal sealed class AccountRegistration
    {
        private readonly IHttpTransport http;
        private readonly IProtocolCodec codec;
        private readonly IClientLog log;
        private readonly AccountRoutes routes;
        private readonly AccountResponseReader responses;

        /// <summary>Cria o cadastro sobre o transporte, sem sessão.</summary>
        /// <example><code>AccountRegistration registration = new AccountRegistration(http, codec, log, routes);</code></example>
        public AccountRegistration(IHttpTransport http, IProtocolCodec codec, IClientLog log, AccountRoutes routes)
        {
            this.http = http ?? throw new ArgumentNullException(nameof(http), "registration transport is null: expected the http transport");
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec), "registration codec is null: expected the project codec");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "registration log is null: expected the client log");
            this.routes = routes ?? throw new ArgumentNullException(nameof(routes), "registration routes is null: expected the account routes");
            responses = new AccountResponseReader(codec, log);
        }

        /// <summary>Envia o cadastro.</summary>
        /// <example><code>AccountCallOutcome&lt;AccountCreated, RegistrationRefusal&gt; registered = await registration.RegisterAsync(form);</code></example>
        public async Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>> RegisterAsync(RegistrationForm form)
        {
            string body = codec.EncodeObject(writer => AccountRequestBodies.WriteRegistration(writer, form));
            HttpOutcome outcome = await http.SendAsync(AccountRequestBodies.JsonPost(routes.Register, body)).ConfigureAwait(false);
            if (outcome.AsFailure is TransportFailure failure)
                return AccountCallOutcome<AccountCreated, RegistrationRefusal>.Failed(AccountCallFailure.TransportFailed(failure));

            HttpResponse response = outcome.AsResponse!;
            if (response.Status != 201)
                return AccountCallOutcome<AccountCreated, RegistrationRefusal>.Refused(ReadRefusal(response));

            log.Info("account_registered");
            return AccountCallOutcome<AccountCreated, RegistrationRefusal>.Success(AccountCreated.Instance);
        }

        private RegistrationRefusal ReadRefusal(HttpResponse response)
        {
            DecodeOutcome<RegistrationRefusal> fields = response.Status == 400 ? responses.Decode(response.Body, ReadFields) : NotFieldErrors();
            if (fields.IsValid)
                return fields.Value;

            log.Warning("account_registration_unrecognized", new LogField("status", response.Status));
            return RegistrationRefusal.FromUnrecognized(new UnrecognizedRefusal(response.Status, response.Body));
        }

        private static RegistrationRefusal ReadFields(IPayloadReader body)
        {
            RegistrationFieldError[] errors = body.FieldNames.Select(name => RegistrationFieldError.Create(name, body.ReadTextList(name))).ToArray();
            if (errors.Length > 0)
                return RegistrationRefusal.FromFields(errors);

            throw new PayloadShapeException(new DecodeFailure(DecodeFailureKind.MissingField, string.Empty, "registration 400 body has no fields: expected field names with lists of messages"));
        }

        private static DecodeOutcome<RegistrationRefusal> NotFieldErrors()
        {
            return DecodeOutcome<RegistrationRefusal>.Invalid(new DecodeFailure(DecodeFailureKind.InvalidValue, string.Empty, "status is not 400: expected field errors only in 400"));
        }
    }
}
