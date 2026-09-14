#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Falha comum de uma chamada de conta (FR-027): sem sessão, transporte, resposta fora do
    /// contrato, ou renovação indisponível. Recusa do próprio recurso (deck inexistente, página
    /// além do fim) não é falha; vai no <c>Refusal</c> de <see cref="AccountCallOutcome{TValue,TRefusal}"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallFailure failure = AccountCallFailure.SessionUnavailable(SessionUnavailableKind.Expired);
    /// </code>
    /// </example>
    public sealed class AccountCallFailure
    {
        private AccountCallFailure(AccountCallFailureKind kind, SessionUnavailableKind? session = null, TransportFailure? transport = null,
            int status = 0, DecodeFailure? decode = null, RenewalOutcome? renewal = null)
        {
            Kind = kind;
            Session = session;
            Transport = transport;
            Status = status;
            Decode = decode;
            Renewal = renewal;
        }

        /// <summary>Qual das falhas.</summary>
        /// <example><code>AccountCallFailureKind kind = failure.Kind;</code></example>
        public AccountCallFailureKind Kind { get; }

        /// <summary>Por que não há sessão; só em <see cref="AccountCallFailureKind.SessionUnavailable"/>.</summary>
        /// <example><code>SessionUnavailableKind? session = failure.Session;</code></example>
        public SessionUnavailableKind? Session { get; }

        /// <summary>A falha do transporte; só em <see cref="AccountCallFailureKind.TransportFailed"/>.</summary>
        /// <example><code>TransportFailureKind? transport = failure.Transport?.Kind;</code></example>
        public TransportFailure? Transport { get; }

        /// <summary>Status da resposta fora do contrato; 0 nos outros casos.</summary>
        /// <example><code>int status = failure.Status;</code></example>
        public int Status { get; }

        /// <summary>O que não bateu com o contrato; só em <see cref="AccountCallFailureKind.OutOfContract"/>.</summary>
        /// <example><code>string? path = failure.Decode?.Path;</code></example>
        public DecodeFailure? Decode { get; }

        /// <summary>A renovação que não saiu; só em <see cref="AccountCallFailureKind.RenewalUnavailable"/>.</summary>
        /// <example><code>RenewalUnavailableReason? reason = failure.Renewal?.Reason;</code></example>
        public RenewalOutcome? Renewal { get; }

        /// <summary>Sem token para usar.</summary>
        /// <example><code>return AccountCallFailure.SessionUnavailable(SessionUnavailableKind.NoSession);</code></example>
        public static AccountCallFailure SessionUnavailable(SessionUnavailableKind session)
        {
            return new AccountCallFailure(AccountCallFailureKind.SessionUnavailable, session: session);
        }

        /// <summary>O transporte falhou.</summary>
        /// <example><code>return AccountCallFailure.TransportFailed(outcome.AsFailure!);</code></example>
        public static AccountCallFailure TransportFailed(TransportFailure transport)
        {
            TransportFailure required = transport ?? throw new ArgumentNullException(nameof(transport), "transport failure is null: expected the failure from IHttpTransport");
            return new AccountCallFailure(AccountCallFailureKind.TransportFailed, transport: required);
        }

        /// <summary>A resposta não segue o contrato.</summary>
        /// <example><code>return AccountCallFailure.OutOfContract(200, decoded.Failure);</code></example>
        public static AccountCallFailure OutOfContract(int status, DecodeFailure decode)
        {
            DecodeFailure required = decode ?? throw new ArgumentNullException(nameof(decode), $"decode failure of http {status} is null: expected what did not match the contract");
            return new AccountCallFailure(AccountCallFailureKind.OutOfContract, status: status, decode: required);
        }

        /// <summary>A renovação necessária não saiu (status do servidor ou resposta fora do contrato).</summary>
        /// <example><code>return AccountCallFailure.RenewalUnavailable(renewal);</code></example>
        public static AccountCallFailure RenewalUnavailable(RenewalOutcome renewal)
        {
            RenewalOutcome required = renewal ?? throw new ArgumentNullException(nameof(renewal), "unavailable renewal is null: expected the renewal outcome that did not succeed");
            return new AccountCallFailure(AccountCallFailureKind.RenewalUnavailable, renewal: required);
        }

        /// <summary>Forma para log, sem corpo nem token.</summary>
        /// <example><code>string text = failure.ToString(); // OutOfContract http 200: MissingField at cards</code></example>
        public override string ToString()
        {
            if (Kind == AccountCallFailureKind.SessionUnavailable)
                return $"{Kind} {Session}";

            if (Kind == AccountCallFailureKind.TransportFailed)
                return $"{Kind} {Transport!.Kind}";

            return Kind == AccountCallFailureKind.OutOfContract ? $"{Kind} http {Status}: {Decode!.Kind} at {Decode.Path}" : $"{Kind} {Renewal}";
        }
    }
}
