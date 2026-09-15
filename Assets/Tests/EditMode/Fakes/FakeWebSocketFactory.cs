#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>Entrega <see cref="FakeWebSocket"/> novos e guarda cada um, para o teste roteirizar a tentativa da vez.</summary>
    /// <example>
    /// <code>
    /// connection.Connect(target);
    /// sockets.Latest.SimulateOpened();
    /// </code>
    /// </example>
    internal sealed class FakeWebSocketFactory : IWebSocketFactory
    {
        private readonly List<FakeWebSocket> created = new List<FakeWebSocket>();

        /// <summary>Todos os sockets criados, na ordem.</summary>
        /// <example><code>Assert.That(sockets.Created.Count, Is.EqualTo(2));</code></example>
        public IReadOnlyList<FakeWebSocket> Created => created;

        /// <summary>O último socket criado.</summary>
        /// <example><code>sockets.Latest.SimulateClosed(4001, "auth_denied");</code></example>
        public FakeWebSocket Latest => created.Count > 0
            ? created[created.Count - 1]
            : throw new InvalidOperationException("FakeWebSocketFactory.Latest with no socket created: expected the code under test to call Create() first");

        /// <inheritdoc />
        public IWebSocket Create()
        {
            FakeWebSocket socket = new FakeWebSocket();
            created.Add(socket);
            return socket;
        }
    }
}
