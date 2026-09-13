#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Uma conexão do probe: guarda os frames decodificados, o fechamento e a thread de cada
    /// evento, e expõe tarefas para os passos esperarem.
    /// </summary>
    internal sealed class ProbeSocketSession
    {
        private readonly IWebSocket socket;
        private readonly IProtocolCodec codec;
        private readonly List<ServerFrame> frames = new List<ServerFrame>();
        private readonly HashSet<int> threadIds = new HashSet<int>();
        private readonly TaskCompletionSource<bool> opened = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<SocketClosure> closed = new TaskCompletionSource<SocketClosure>();
        private readonly TaskCompletionSource<PongFrame> firstPong = new TaskCompletionSource<PongFrame>();

        private ProbeSocketSession(IWebSocket socket, IProtocolCodec codec)
        {
            this.socket = socket;
            this.codec = codec;
            socket.Opened += () => Observe(() => opened.TrySetResult(true));
            socket.TextReceived += text => Observe(() => Record(text));
            socket.Closed += closure => Observe(() => EndWith(closure));
        }

        internal Task Opened => opened.Task;

        internal Task<SocketClosure> Closed => closed.Task;

        internal Task<PongFrame> FirstPong => firstPong.Task;

        internal IReadOnlyList<ServerFrame> Frames => frames;

        internal IReadOnlyCollection<int> ThreadIds => threadIds;

        internal static ProbeSocketSession Open(LiveNetworkAdapters adapters, Uri url)
        {
            ProbeSocketSession session = new ProbeSocketSession(adapters.CreateSocket(), adapters.Codec);
            session.socket.Open(url);
            return session;
        }

        internal Task<SocketSendOutcome> SendAsync(IOutgoingMessage message) => socket.SendTextAsync(codec.Encode(message));

        internal void Close() => socket.Close();

        private void Observe(Action record)
        {
            threadIds.Add(Thread.CurrentThread.ManagedThreadId);
            record();
        }

        private void Record(string text)
        {
            DecodeOutcome<ServerFrame> outcome = codec.Decode(text);
            if (!outcome.IsValid)
                return;

            frames.Add(outcome.Value);
            if (outcome.Value is PongFrame pong)
                firstPong.TrySetResult(pong);
        }

        private void EndWith(SocketClosure closure)
        {
            opened.TrySetResult(false);
            closed.TrySetResult(closure);
        }

        internal bool Denied() => frames.OfType<AuthDeniedFrame>().Any();
    }
}
