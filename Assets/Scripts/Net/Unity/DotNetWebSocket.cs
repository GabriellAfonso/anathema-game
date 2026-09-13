#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// <see cref="IWebSocket"/> sobre o <c>ClientWebSocket</c> do .NET, que entrega o código de
    /// fechamento exato (a NativeWebSocket achatava 4001/4403/4404). A recepção roda fora da
    /// thread principal; todo aviso passa pela <see cref="MainThreadQueue"/>
    /// (specs/001-server-connection/research.md, R4).
    /// </summary>
    /// <example>
    /// <code>
    /// IWebSocket socket = new DotNetWebSocket(queue, policy, log);
    /// socket.Closed += closure => log.Info("socket_closed", new LogField("close_code", closure.Code ?? 0));
    /// socket.Open(new Uri(wsBase + "/ws/matchmaking/?token=" + token));
    /// </code>
    /// </example>
    public sealed class DotNetWebSocket : IWebSocket
    {
        private const int StateIdle = 0, StateOpening = 1, StateOpen = 2, StateClosed = 3;
        private const int ReceiveChunkBytes = 8192;
        private const string ClosedByClientReason = "closed_by_client";
        private static readonly TimeSpan CloseHandshakeTimeout = TimeSpan.FromSeconds(5);

        private readonly MainThreadQueue queue;
        private readonly CleartextPolicy policy;
        private readonly IClientLog log;
        private readonly ClientWebSocket socket = new ClientWebSocket();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly SemaphoreSlim sendGate = new SemaphoreSlim(1, 1);
        private readonly object publishGate = new object();
        private int state = StateIdle;
        private int closeRequested;
        private bool closedPublished;

        /// <summary>Cria a conexão ainda fechada.</summary>
        /// <example><code>IWebSocket socket = new DotNetWebSocket(queue, new CleartextPolicy(Debug.isDebugBuild), log);</code></example>
        public DotNetWebSocket(MainThreadQueue queue, CleartextPolicy policy, IClientLog log)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue), "queue is null: expected the main thread queue");
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy), "policy is null: expected the cleartext policy of this build");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        }

        /// <summary>Handshake concluído.</summary>
        public event Action? Opened;

        /// <summary>Texto inteiro do servidor.</summary>
        public event Action<string>? TextReceived;

        /// <summary>A conexão terminou.</summary>
        public event Action<SocketClosure>? Closed;

        /// <summary>Falha do transporte ou frame binário.</summary>
        public event Action<string>? Errored;

        private bool IsClosed
        {
            get
            {
                lock (publishGate)
                    return closedPublished;
            }
        }

        /// <summary>Inicia a conexão fora da thread principal.</summary>
        /// <example><code>socket.Open(new Uri("ws://192.168.0.10:8000/ws/matchmaking/?token=" + token));</code></example>
        public void Open(Uri url)
        {
            if (url == null || !url.IsAbsoluteUri)
                throw new ArgumentException($"socket url is '{url}': expected an absolute ws or wss url", nameof(url));

            if (Interlocked.CompareExchange(ref state, StateOpening, StateIdle) != StateIdle)
                throw new InvalidOperationException($"DotNetWebSocket.Open while state {state}: expected a new instance, one instance is one connection");

            if (!policy.Permits(url))
            {
                RefuseCleartext(url);
                return;
            }

            _ = Task.Run(() => RunConnectionAsync(url));
        }

        /// <summary>Envia texto; sem conexão aberta devolve <see cref="SocketSendOutcome.NotOpen"/>.</summary>
        /// <example><code>SocketSendOutcome outcome = await socket.SendTextAsync(codec.Encode(ping));</code></example>
        public async Task<SocketSendOutcome> SendTextAsync(string text)
        {
            if (Volatile.Read(ref state) != StateOpen || IsClosed)
                return SocketSendOutcome.NotOpen;

            // O ClientWebSocket não aceita dois SendAsync ao mesmo tempo.
            await sendGate.WaitAsync().ConfigureAwait(false);
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, lifetime.Token).ConfigureAwait(false);
                return SocketSendOutcome.Sent;
            }
            catch (Exception failure)
            {
                return SocketSendOutcome.Failed($"{failure.GetType().Name}: {failure.Message}");
            }
            finally
            {
                sendGate.Release();
            }
        }

        /// <summary>Fecha de propósito; antes de abrir, fecha na hora. Idempotente.</summary>
        /// <example><code>socket.Close();</code></example>
        public void Close()
        {
            if (TryCloseBeforeOpen())
                return;

            if (Volatile.Read(ref state) != StateOpen || Interlocked.Exchange(ref closeRequested, 1) == 1)
                return;

            _ = Task.Run(CloseHandshakeAsync);
        }

        private bool TryCloseBeforeOpen()
        {
            bool wasIdle = Interlocked.CompareExchange(ref state, StateClosed, StateIdle) == StateIdle;
            if (!wasIdle && Interlocked.CompareExchange(ref state, StateClosed, StateOpening) != StateOpening)
                return false;

            lifetime.Cancel();
            PublishClosed(null, SocketClosure.ClosedBeforeOpenReason);
            return true;
        }

        private void RefuseCleartext(Uri url)
        {
            log.Error("socket_cleartext_refused", new LogField("url", url.GetLeftPart(UriPartial.Path)));
            Publish(() => Errored?.Invoke(SocketClosure.CleartextRefusedReason));
            PublishClosed(null, SocketClosure.CleartextRefusedReason);
        }

        private async Task RunConnectionAsync(Uri url)
        {
            try
            {
                await socket.ConnectAsync(url, lifetime.Token).ConfigureAwait(false);
                if (Interlocked.CompareExchange(ref state, StateOpen, StateOpening) != StateOpening)
                    return;

                Publish(() => Opened?.Invoke());
                await ReceiveLoopAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                OnTransportFailure(failure);
            }
        }

        private async Task ReceiveLoopAsync()
        {
            byte[] chunk = new byte[ReceiveChunkBytes];
            WebSocketMessageAssembler assembler = new WebSocketMessageAssembler();
            while (!IsClosed)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(chunk), lifetime.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    OnCloseFrame(result);
                    return;
                }

                assembler.Append(new ArraySegment<byte>(chunk, 0, result.Count));
                if (result.EndOfMessage)
                    Deliver(result.MessageType, assembler.Complete());
            }
        }

        private void Deliver(WebSocketMessageType messageType, string text)
        {
            if (messageType == WebSocketMessageType.Binary)
            {
                log.Warning("socket_binary_frame_ignored", new LogField("chars", text.Length));
                Publish(() => Errored?.Invoke("binary_frame_ignored"));
                return;
            }

            Publish(() => TextReceived?.Invoke(text));
        }

        private void OnCloseFrame(WebSocketReceiveResult result)
        {
            // Close frame sem status chega como Empty (1005): para o cliente, é "sem código".
            WebSocketCloseStatus? status = result.CloseStatus;
            int? code = status.HasValue && status.Value != WebSocketCloseStatus.Empty ? (int)status.Value : (int?)null;
            PublishClosed(code, result.CloseStatusDescription ?? string.Empty);
        }

        private void OnTransportFailure(Exception failure)
        {
            if (IsClosed)
                return;

            string detail = $"{failure.GetType().Name}: {failure.Message}";
            log.Warning("socket_transport_failed", new LogField("detail", detail));
            Publish(() => Errored?.Invoke(detail));
            PublishClosed(null, SocketClosure.AbnormalReason);
        }

        private async Task CloseHandshakeAsync()
        {
            try
            {
                using CancellationTokenSource timeout = new CancellationTokenSource(CloseHandshakeTimeout);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosedByClientReason, timeout.Token).ConfigureAwait(false);
                await Task.Delay(CloseHandshakeTimeout).ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                log.Debug("socket_close_handshake_failed", new LogField("detail", failure.Message));
            }

            // O servidor não ecoou o close a tempo: derruba e avisa com o código que o cliente mandou.
            socket.Abort();
            PublishClosed((int)WebSocketCloseStatus.NormalClosure, ClosedByClientReason);
        }

        private void Publish(Action notify)
        {
            lock (publishGate)
            {
                if (!closedPublished)
                    queue.Enqueue(notify);
            }
        }

        private void PublishClosed(int? code, string reason)
        {
            lock (publishGate)
            {
                if (closedPublished)
                    return;

                closedPublished = true;
                Volatile.Write(ref state, StateClosed);
                SocketClosure closure = new SocketClosure(code, reason);
                queue.Enqueue(() => Closed?.Invoke(closure));
            }

            log.Info("socket_closed", new LogField("close_code", CloseCodeText(code)), new LogField("reason", reason));
        }

        private static string CloseCodeText(int? code) => code.HasValue ? code.Value.ToString() : "none";
    }
}
