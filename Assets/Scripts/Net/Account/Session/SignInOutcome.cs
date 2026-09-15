#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de <see cref="AccountSession.SignInAsync"/>. Credencial recusada, outro status,
    /// transporte e resposta fora do contrato são distinguíveis (FR-004); nenhum é exceção.
    /// </summary>
    /// <example>
    /// <code>
    /// SignInOutcome outcome = await session.SignInAsync(username, new Password(text));
    /// if (outcome.Kind == SignInOutcomeKind.SignedIn) LoadHome();
    /// </code>
    /// </example>
    public sealed class SignInOutcome
    {
        private SignInOutcome(SignInOutcomeKind kind, UserId? user = null, int status = 0, TransportFailure? transport = null, DecodeFailure? decode = null)
        {
            Kind = kind;
            User = user;
            Status = status;
            Transport = transport;
            Decode = decode;
        }

        /// <summary>Como terminou.</summary>
        /// <example><code>SignInOutcomeKind kind = outcome.Kind;</code></example>
        public SignInOutcomeKind Kind { get; }

        /// <summary>Quem entrou; só em <see cref="SignInOutcomeKind.SignedIn"/>.</summary>
        /// <example><code>UserId? self = outcome.User;</code></example>
        public UserId? User { get; }

        /// <summary>Status recusado; só em <see cref="SignInOutcomeKind.ServerRefused"/>.</summary>
        /// <example><code>int status = outcome.Status;</code></example>
        public int Status { get; }

        /// <summary>A falha de transporte; só em <see cref="SignInOutcomeKind.TransportFailed"/>.</summary>
        /// <example><code>TransportFailureKind? kind = outcome.Transport?.Kind;</code></example>
        internal TransportFailure? Transport { get; }

        /// <summary>O que não bateu; só em <see cref="SignInOutcomeKind.OutOfContract"/>. Nunca contém token.</summary>
        /// <example><code>string? path = outcome.Decode?.Path;</code></example>
        public DecodeFailure? Decode { get; }

        /// <summary>Entrou.</summary>
        /// <example><code>return SignInOutcome.SignedIn(token.Owner);</code></example>
        public static SignInOutcome SignedIn(UserId user) => new SignInOutcome(SignInOutcomeKind.SignedIn, user: user);

        /// <summary>Credencial recusada.</summary>
        /// <example><code>return SignInOutcome.CredentialsRefused();</code></example>
        public static SignInOutcome CredentialsRefused() => new SignInOutcome(SignInOutcomeKind.CredentialsRefused, status: 401);

        /// <summary>Outro status do servidor.</summary>
        /// <example><code>return SignInOutcome.ServerRefused(500);</code></example>
        public static SignInOutcome ServerRefused(int status) => new SignInOutcome(SignInOutcomeKind.ServerRefused, status: status);

        /// <summary>Falha de transporte.</summary>
        /// <example><code>return SignInOutcome.TransportFailed(outcome.AsFailure!);</code></example>
        internal static SignInOutcome TransportFailed(TransportFailure transport)
        {
            return new SignInOutcome(SignInOutcomeKind.TransportFailed, transport: transport ?? throw new ArgumentNullException(nameof(transport), "sign in transport failure is null: expected the failure from IHttpTransport"));
        }

        /// <summary>Resposta sem tokens legíveis.</summary>
        /// <example><code>return SignInOutcome.OutOfContract(tokens.Failure);</code></example>
        public static SignInOutcome OutOfContract(DecodeFailure decode)
        {
            return new SignInOutcome(SignInOutcomeKind.OutOfContract, status: 200, decode: decode ?? throw new ArgumentNullException(nameof(decode), "sign in decode failure is null: expected what did not match"));
        }

        /// <summary>Já havia um login em curso.</summary>
        /// <example><code>return SignInOutcome.AlreadyInProgress();</code></example>
        public static SignInOutcome AlreadyInProgress() => new SignInOutcome(SignInOutcomeKind.AlreadyInProgress);

        /// <summary>Já havia sessão com este dono; nada foi enviado.</summary>
        /// <example><code>return SignInOutcome.AlreadySignedIn(session.Self!.Value);</code></example>
        public static SignInOutcome AlreadySignedIn(UserId user) => new SignInOutcome(SignInOutcomeKind.AlreadySignedIn, user: user);

        /// <summary>Forma para log, sem credencial.</summary>
        /// <example><code>string text = outcome.ToString(); // SignedIn user_id=7</code></example>
        public override string ToString()
        {
            if (Kind == SignInOutcomeKind.SignedIn)
                return $"{Kind} {User}";

            if (Kind == SignInOutcomeKind.AlreadySignedIn)
                return $"{Kind} {User}";

            return Decode == null ? $"{Kind} status={Status}" : $"{Kind} {Decode.Kind} at {Decode.Path}";
        }
    }
}
