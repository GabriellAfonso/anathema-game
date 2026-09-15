#nullable enable
using System;
using System.Threading.Tasks;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Uma conexão de socket de texto. Uma instância é uma conexão: reabrir é criar
    /// outra. No adaptador real todo aviso chega na thread principal; no fake,
    /// na hora, na thread do teste. Garantias em
    /// specs/001-server-connection/contracts/core-ports.md.
    /// </summary>
    /// <example>
    /// <code>
    /// socket.TextReceived += text => frames.Add(codec.Decode(text));
    /// socket.Closed += closure => log.Info("socket_closed", new LogField("close_code", closure.Code ?? 0));
    /// socket.Open(new Uri("ws://127.0.0.1:8000/ws/matchmaking/?token=" + token));
    /// </code>
    /// </example>
    internal interface IWebSocket
    {
        /// <summary>Handshake concluído.</summary>
        event Action? Opened;

        /// <summary>Um texto inteiro do servidor, na ordem de chegada.</summary>
        event Action<string>? TextReceived;

        /// <summary>A conexão terminou. No máximo uma vez; nada chega depois.</summary>
        event Action<SocketClosure>? Closed;

        /// <summary>Falha do transporte ou frame que o cliente não aceita (binário).</summary>
        event Action<string>? Errored;

        /// <summary>Inicia a conexão. Chamar fora do estado inicial lança <see cref="InvalidOperationException"/>.</summary>
        /// <example><code>socket.Open(new Uri("wss://api.anathema.com/ws/matchmaking/?token=" + token));</code></example>
        void Open(Uri url);

        /// <summary>Envia um texto. Sem conexão aberta devolve <see cref="SocketSendOutcome.NotOpen"/>, sem exceção.</summary>
        /// <example><code>SocketSendOutcome outcome = await socket.SendTextAsync(codec.Encode(new PingMessage(null)));</code></example>
        Task<SocketSendOutcome> SendTextAsync(string text);

        /// <summary>Fecha de propósito. Idempotente em qualquer estado.</summary>
        /// <example><code>socket.Close();</code></example>
        void Close();
    }
}
