#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O que aconteceu com um envio. Mandar sem conexão aberta é resultado, não
    /// exceção (FR-008): quem chama decide se guarda a mensagem ou desiste.
    /// </summary>
    /// <example>
    /// <code>
    /// SocketSendOutcome outcome = await socket.SendTextAsync(frame);
    /// if (outcome.Status != SocketSendStatus.Sent) log.Warning("send_skipped");
    /// </code>
    /// </example>
    public sealed class SocketSendOutcome
    {
        /// <summary>Envio feito.</summary>
        /// <example><code>return SocketSendOutcome.Sent;</code></example>
        public static readonly SocketSendOutcome Sent = new SocketSendOutcome(SocketSendStatus.Sent, null);

        /// <summary>Conexão não estava aberta.</summary>
        /// <example><code>return SocketSendOutcome.NotOpen;</code></example>
        public static readonly SocketSendOutcome NotOpen = new SocketSendOutcome(SocketSendStatus.NotOpen, null);

        private SocketSendOutcome(SocketSendStatus status, string? failureDetail)
        {
            Status = status;
            FailureDetail = failureDetail;
        }

        /// <summary>Qual dos três resultados.</summary>
        /// <example><code>bool sent = outcome.Status == SocketSendStatus.Sent;</code></example>
        public SocketSendStatus Status { get; }

        /// <summary>Descrição da falha do transporte; nula fora de <see cref="SocketSendStatus.Failed"/>.</summary>
        /// <example><code>string? why = outcome.FailureDetail;</code></example>
        public string? FailureDetail { get; }

        /// <summary>Falha do transporte com a descrição recebida dele.</summary>
        /// <example><code>return SocketSendOutcome.Failed(exception.Message);</code></example>
        public static SocketSendOutcome Failed(string detail)
        {
            if (detail == null)
                throw new ArgumentNullException(nameof(detail), "send failure detail is null: expected the transport error text");

            return new SocketSendOutcome(SocketSendStatus.Failed, detail);
        }
    }
}
