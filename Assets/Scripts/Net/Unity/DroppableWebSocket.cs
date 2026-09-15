#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Um socket que repassa tudo do socket de dentro e sabe ser derrubado: ao derrubar, deixa de ouvir o de dentro,
    /// pede o fechamento dele e avisa ele mesmo o fim sem código de fechamento, como uma rede que caiu.
    /// </summary>
    internal sealed class DroppableWebSocket : IWebSocket
    {
        internal const string DroppedReason = "dropped_by_composition";

        private readonly IWebSocket inner;
        private readonly Action<DroppableWebSocket> forget;
        private bool finished;

        internal DroppableWebSocket(IWebSocket inner, Action<DroppableWebSocket> forget)
        {
            this.inner = inner;
            this.forget = forget;
            inner.Opened += OnOpened;
            inner.TextReceived += OnText;
            inner.Closed += OnClosed;
            inner.Errored += OnErrored;
        }

        public event Action? Opened;

        public event Action<string>? TextReceived;

        public event Action<SocketClosure>? Closed;

        public event Action<string>? Errored;

        public void Open(Uri url) => inner.Open(url);

        public Task<SocketSendOutcome> SendTextAsync(string text) => inner.SendTextAsync(text);

        public void Close() => inner.Close();

        internal void Drop()
        {
            if (finished)
                return;

            Detach();
            inner.Close();
            Finish(new SocketClosure(null, DroppedReason));
        }

        private void OnOpened() => Opened?.Invoke();

        private void OnText(string text) => TextReceived?.Invoke(text);

        private void OnErrored(string detail) => Errored?.Invoke(detail);

        private void OnClosed(SocketClosure closure)
        {
            Detach();
            Finish(closure);
        }

        private void Finish(SocketClosure closure)
        {
            if (finished)
                return;

            finished = true;
            forget(this);
            Closed?.Invoke(closure);
        }

        private void Detach()
        {
            inner.Opened -= OnOpened;
            inner.TextReceived -= OnText;
            inner.Closed -= OnClosed;
            inner.Errored -= OnErrored;
        }
    }
}
