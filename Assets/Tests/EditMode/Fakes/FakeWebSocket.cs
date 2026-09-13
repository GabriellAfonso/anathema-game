#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Socket roteirizado pelo teste. Segue a mesma máquina de estados do adaptador
    /// real (specs/001-server-connection/data-model.md, "Socket"): roteiro fora de
    /// ordem lança, para o teste não passar por um caminho que o real nunca faz.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeWebSocket socket = new FakeWebSocket();
    /// socket.Open(new Uri("ws://127.0.0.1:8000/ws/matchmaking/"));
    /// socket.SimulateOpened();
    /// socket.SimulateClosed(4001, "auth_denied");
    /// </code>
    /// </example>
    public sealed class FakeWebSocket : IWebSocket
    {
        private enum State { Idle, Opening, Open, Closed }

        private readonly List<string> sentTexts = new List<string>();
        private State state = State.Idle;

        /// <summary>Handshake concluído.</summary>
        public event Action? Opened;

        /// <summary>Texto do servidor.</summary>
        public event Action<string>? TextReceived;

        /// <summary>Conexão terminou.</summary>
        public event Action<SocketClosure>? Closed;

        /// <summary>Falha do transporte.</summary>
        public event Action<string>? Errored;

        /// <summary>URL passada a <see cref="Open"/>, ou nula.</summary>
        /// <example><code>Assert.That(socket.OpenedUrl, Is.EqualTo(expected));</code></example>
        public Uri? OpenedUrl { get; private set; }

        /// <summary>Textos enviados com sucesso, em ordem.</summary>
        /// <example><code>string ping = socket.SentTexts[0];</code></example>
        public IReadOnlyList<string> SentTexts => sentTexts;

        /// <summary>Quantas vezes <see cref="Close"/> foi chamado.</summary>
        /// <example><code>Assert.That(socket.CloseRequests, Is.EqualTo(1));</code></example>
        public int CloseRequests { get; private set; }

        /// <summary>Resultado do próximo envio com conexão aberta; padrão <see cref="SocketSendOutcome.Sent"/>.</summary>
        /// <example><code>socket.NextSendOutcome = SocketSendOutcome.Failed("broken pipe");</code></example>
        public SocketSendOutcome NextSendOutcome { get; set; } = SocketSendOutcome.Sent;

        /// <summary>Registra a URL e passa a abrir.</summary>
        /// <example><code>socket.Open(url);</code></example>
        public void Open(Uri url)
        {
            if (state != State.Idle)
                throw new InvalidOperationException($"FakeWebSocket.Open while {state}: expected Idle, one instance is one connection");

            OpenedUrl = url;
            state = State.Opening;
        }

        /// <summary>Envia se aberto; senão <see cref="SocketSendOutcome.NotOpen"/>.</summary>
        /// <example><code>SocketSendOutcome outcome = socket.SendTextAsync("{}").Result;</code></example>
        public Task<SocketSendOutcome> SendTextAsync(string text)
        {
            if (state != State.Open)
                return Task.FromResult(SocketSendOutcome.NotOpen);

            if (NextSendOutcome.Status == SocketSendStatus.Sent)
                sentTexts.Add(text);

            return Task.FromResult(NextSendOutcome);
        }

        /// <summary>Pede o fechamento. Antes de abrir, fecha na hora com motivo local.</summary>
        /// <example><code>socket.Close();</code></example>
        public void Close()
        {
            CloseRequests++;
            if (state == State.Idle || state == State.Opening)
                FinishClosed(null, SocketClosure.ClosedBeforeOpenReason);
        }

        /// <summary>Simula o handshake concluído.</summary>
        /// <example><code>socket.SimulateOpened();</code></example>
        public void SimulateOpened()
        {
            Require(state == State.Opening, "SimulateOpened");
            state = State.Open;
            Opened?.Invoke();
        }

        /// <summary>Simula um texto do servidor.</summary>
        /// <example><code>socket.SimulateText("{\"type\": \"pong\", \"payload\": {}}");</code></example>
        public void SimulateText(string text)
        {
            Require(state == State.Open, "SimulateText");
            TextReceived?.Invoke(text);
        }

        /// <summary>Simula uma falha do transporte.</summary>
        /// <example><code>socket.SimulateError("connection reset");</code></example>
        public void SimulateError(string detail)
        {
            Require(state != State.Closed, "SimulateError");
            Errored?.Invoke(detail);
        }

        /// <summary>Simula o fim da conexão; <paramref name="code"/> nulo é queda sem close frame.</summary>
        /// <example><code>socket.SimulateClosed(4001, "auth_denied");</code></example>
        public void SimulateClosed(int? code, string reason)
        {
            Require(state != State.Closed, "SimulateClosed");
            FinishClosed(code, reason);
        }

        private void FinishClosed(int? code, string reason)
        {
            if (state == State.Closed)
                return;

            state = State.Closed;
            Closed?.Invoke(new SocketClosure(code, reason));
        }

        private void Require(bool allowed, string action)
        {
            if (!allowed)
                throw new InvalidOperationException($"FakeWebSocket.{action} while {state}: script the socket in order Open, SimulateOpened, SimulateText/SimulateError, SimulateClosed");
        }
    }
}
