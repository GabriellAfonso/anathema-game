#nullable enable
using System;
using Anathema.Net.Connection;

namespace Anathema.Net.Match
{
    /// <summary>Resultado de mandar um comando: o estado do envio, o comando e a fase da conexão naquele momento.</summary>
    /// <example>
    /// <code>
    /// PlaySendResult result = await match.Commands.Pass();
    /// if (result.Status != PlaySendStatus.Sent) log.Warning("pass_not_sent", new LogField("phase", result.ConnectionPhase.ToString()));
    /// </code>
    /// </example>
    public sealed class PlaySendResult
    {
        internal PlaySendResult(PlaySendStatus status, PlayCommand command, ConnectionPhase connectionPhase)
        {
            Status = status;
            Command = command ?? throw new ArgumentNullException(nameof(command), $"sent command is null for status {status}: expected the command that was tried");
            ConnectionPhase = connectionPhase;
        }

        /// <summary>Enviado ou por que não.</summary>
        /// <example><code>bool sent = result.Status == PlaySendStatus.Sent;</code></example>
        public PlaySendStatus Status { get; }

        /// <summary>O comando tentado.</summary>
        /// <example><code>string type = result.Command.MessageType;</code></example>
        public PlayCommand Command { get; }

        /// <summary>A fase da conexão quando o envio foi decidido.</summary>
        /// <example><code>ConnectionPhase phase = result.ConnectionPhase;</code></example>
        public ConnectionPhase ConnectionPhase { get; }
    }
}
