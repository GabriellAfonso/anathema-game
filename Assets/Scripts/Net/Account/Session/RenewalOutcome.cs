#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de uma renovação do token de acesso. Só 401 expira a sessão: um 400 por bug do
    /// cliente, um 503 ou falta de rede não podem deslogar o jogador (research R5).
    /// </summary>
    /// <example>
    /// <code>
    /// RenewalOutcome renewed = await tokens.RenewNowAsync();
    /// if (renewed.Kind == RenewalOutcomeKind.Renewed) Reconnect(renewed.Token!);
    /// </code>
    /// </example>
    internal sealed class RenewalOutcome
    {
        private RenewalOutcome(RenewalOutcomeKind kind, AccessToken? token = null, RenewalUnavailableReason? reason = null, TransportFailure? transport = null, string detail = "")
        {
            Kind = kind;
            Token = token;
            Reason = reason;
            Transport = transport;
            Detail = detail;
        }

        /// <summary>Como terminou.</summary>
        /// <example><code>RenewalOutcomeKind kind = renewed.Kind;</code></example>
        public RenewalOutcomeKind Kind { get; }

        /// <summary>O token novo; só em <see cref="RenewalOutcomeKind.Renewed"/>.</summary>
        /// <example><code>AccessToken? token = renewed.Token;</code></example>
        public AccessToken? Token { get; }

        /// <summary>Por que não saiu; só em <see cref="RenewalOutcomeKind.Unavailable"/>.</summary>
        /// <example><code>RenewalUnavailableReason? reason = renewed.Reason;</code></example>
        public RenewalUnavailableReason? Reason { get; }

        /// <summary>A falha de transporte, quando o motivo é transporte.</summary>
        /// <example><code>TransportFailureKind? kind = renewed.Transport?.Kind;</code></example>
        public TransportFailure? Transport { get; }

        /// <summary>Detalhe para log; nunca contém token.</summary>
        /// <example><code>string detail = renewed.Detail; // http 503</code></example>
        public string Detail { get; }

        /// <summary>Token novo.</summary>
        /// <example><code>return RenewalOutcome.Renewed(access.Value);</code></example>
        public static RenewalOutcome Renewed(AccessToken token)
        {
            return new RenewalOutcome(RenewalOutcomeKind.Renewed, token ?? throw new ArgumentNullException(nameof(token), "renewed access token is null: expected the token read from the refresh response"));
        }

        /// <summary>Refresh recusado.</summary>
        /// <example><code>return RenewalOutcome.SessionExpired();</code></example>
        public static RenewalOutcome SessionExpired() => new RenewalOutcome(RenewalOutcomeKind.SessionExpired);

        /// <summary>Nada a renovar.</summary>
        /// <example><code>return RenewalOutcome.NoSession();</code></example>
        public static RenewalOutcome NoSession() => new RenewalOutcome(RenewalOutcomeKind.NoSession);

        /// <summary>Não saiu por transporte.</summary>
        /// <example><code>return RenewalOutcome.TransportFailed(outcome.AsFailure!);</code></example>
        public static RenewalOutcome TransportFailed(TransportFailure transport)
        {
            TransportFailure required = transport ?? throw new ArgumentNullException(nameof(transport), "renewal transport failure is null: expected the failure from IHttpTransport");
            return new RenewalOutcome(RenewalOutcomeKind.Unavailable, reason: RenewalUnavailableReason.Transport, transport: required, detail: required.Kind.ToString());
        }

        /// <summary>Não saiu por status do servidor.</summary>
        /// <example><code>return RenewalOutcome.ServerStatus(503);</code></example>
        public static RenewalOutcome ServerStatus(int status)
        {
            return new RenewalOutcome(RenewalOutcomeKind.Unavailable, reason: RenewalUnavailableReason.ServerStatus, detail: $"http {status}");
        }

        /// <summary>Não saiu porque a resposta não segue o contrato.</summary>
        /// <example><code>return RenewalOutcome.OutOfContract(access.Failure);</code></example>
        public static RenewalOutcome OutOfContract(DecodeFailure decode)
        {
            DecodeFailure required = decode ?? throw new ArgumentNullException(nameof(decode), "renewal decode failure is null: expected what did not match");
            return new RenewalOutcome(RenewalOutcomeKind.Unavailable, reason: RenewalUnavailableReason.OutOfContract, detail: $"{required.Kind} at {required.Path}");
        }

        /// <summary>Forma para log, sem token.</summary>
        /// <example><code>string text = renewed.ToString(); // Unavailable ServerStatus: http 503</code></example>
        public override string ToString()
        {
            return Kind == RenewalOutcomeKind.Unavailable ? $"{Kind} {Reason}: {Detail}" : $"{Kind}";
        }
    }
}
