#nullable enable
using System;
using Anathema.Net.Connection;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>Sockets do backend local de <c>docker compose up</c>, para os testes de adaptador e os LiveServer.</summary>
    internal static class LocalConnectionRoutes
    {
        internal const string WsBase = "ws://127.0.0.1:8000";

        internal static ConnectionRoutes Create()
        {
            return new ConnectionRoutes(new Uri(WsBase + "/ws/matchmaking/"), new Uri(WsBase + "/ws/match/"));
        }
    }
}
