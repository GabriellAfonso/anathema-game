#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O pedido não chegou a ter resposta do servidor. Distinto de 5xx: um 504 de
    /// proxy é resposta; um prazo esgotado sem resposta é isto.
    /// </summary>
    /// <example>
    /// <code>
    /// TransportFailure failure = new TransportFailure(TransportFailureKind.Timeout, "Request timeout");
    /// </code>
    /// </example>
    public sealed class TransportFailure : HttpOutcome
    {
        /// <summary>Cria a falha com a categoria e o texto do transporte.</summary>
        /// <example><code>HttpOutcome offline = new TransportFailure(TransportFailureKind.CannotConnect, error);</code></example>
        public TransportFailure(TransportFailureKind kind, string detail)
        {
            Kind = kind;
            Detail = detail ?? throw new ArgumentNullException(nameof(detail), $"detail of {kind} is null: expected the transport error text");
        }

        /// <summary>Categoria da falha.</summary>
        /// <example><code>bool timedOut = failure.Kind == TransportFailureKind.Timeout;</code></example>
        public TransportFailureKind Kind { get; }

        /// <summary>Texto do transporte, para log.</summary>
        /// <example><code>string detail = failure.Detail;</code></example>
        public string Detail { get; }

        /// <summary>Sempre nulo.</summary>
        /// <example><code>HttpResponse? none = failure.AsResponse;</code></example>
        public override HttpResponse? AsResponse => null;

        /// <summary>Esta própria falha.</summary>
        /// <example><code>TransportFailure? same = failure.AsFailure;</code></example>
        public override TransportFailure? AsFailure => this;
    }
}
