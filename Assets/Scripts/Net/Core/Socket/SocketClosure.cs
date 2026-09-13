#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Como uma conexão de socket terminou. O código chega como o número exato do
    /// close frame (4001, 4400, 4403, 4404…), que a NativeWebSocket achatava; sem
    /// close frame (queda de rede), <see cref="Code"/> é nulo.
    /// </summary>
    /// <example>
    /// <code>
    /// socket.Closed += closure =>
    /// {
    ///     if (closure.Code == 4001) RenewToken();
    /// };
    /// </code>
    /// </example>
    public sealed class SocketClosure
    {
        /// <summary>Motivo local: a conexão caiu sem close frame.</summary>
        /// <example><code>bool dropped = closure.Reason == SocketClosure.AbnormalReason;</code></example>
        public const string AbnormalReason = "abnormal";

        /// <summary>Motivo local: <c>Close()</c> antes de a conexão abrir.</summary>
        /// <example><code>bool cancelled = closure.Reason == SocketClosure.ClosedBeforeOpenReason;</code></example>
        public const string ClosedBeforeOpenReason = "closed_before_open";

        /// <summary>Motivo local: a política de cleartext recusou a URL.</summary>
        /// <example><code>bool refused = closure.Reason == SocketClosure.CleartextRefusedReason;</code></example>
        public const string CleartextRefusedReason = "cleartext_refused";

        /// <summary>Cria o fechamento; <paramref name="code"/> nulo quando não houve close frame.</summary>
        /// <example><code>SocketClosure denied = new SocketClosure(4001, "auth_denied");</code></example>
        public SocketClosure(int? code, string reason)
        {
            Code = code;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason), $"closure reason is null for code {code}: expected the server reason or a local reason constant");
        }

        /// <summary>Número exato do close frame, ou nulo sem close frame.</summary>
        /// <example><code>int? code = closure.Code;</code></example>
        public int? Code { get; }

        /// <summary>Motivo do servidor, ou um dos motivos locais desta classe.</summary>
        /// <example><code>string reason = closure.Reason;</code></example>
        public string Reason { get; }

        /// <summary>Verdadeiro quando o servidor mandou close frame com código.</summary>
        /// <example><code>if (!closure.HasServerCode) Reconnect();</code></example>
        public bool HasServerCode => Code.HasValue;

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = closure.ToString(); // close_code=4001 reason=auth</code></example>
        public override string ToString() => $"close_code={(Code.HasValue ? Code.Value.ToString() : "none")} reason={Reason}";
    }
}
