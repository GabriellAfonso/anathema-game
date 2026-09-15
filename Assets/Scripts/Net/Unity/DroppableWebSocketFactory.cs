#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Decora a fábrica real e guarda os sockets vivos, para a prova derrubar a conexão com os adaptadores reais: o
    /// socket derrubado avisa um fechamento sem código, que a conexão da 003 trata como queda de rede
    /// (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    internal sealed class DroppableWebSocketFactory : IWebSocketFactory
    {
        private readonly IWebSocketFactory inner;
        private readonly object gate = new object();
        private readonly List<DroppableWebSocket> live = new List<DroppableWebSocket>();

        internal DroppableWebSocketFactory(IWebSocketFactory inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner), "inner socket factory is null: expected the DotNetWebSocketFactory");
        }

        internal int LiveCount
        {
            get
            {
                lock (gate)
                    return live.Count;
            }
        }

        public IWebSocket Create()
        {
            DroppableWebSocket socket = new DroppableWebSocket(inner.Create(), Forget);
            lock (gate)
                live.Add(socket);

            return socket;
        }

        internal void DropAll()
        {
            DroppableWebSocket[] current;
            lock (gate)
                current = live.ToArray();

            foreach (DroppableWebSocket socket in current)
                socket.Drop();
        }

        private void Forget(DroppableWebSocket socket)
        {
            lock (gate)
                live.Remove(socket);
        }
    }
}
