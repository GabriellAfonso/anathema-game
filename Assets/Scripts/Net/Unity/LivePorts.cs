#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Facade;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// As portas da fachada sobre os adaptadores reais: HTTP, socket, relógio, fila e codec vêm de
    /// <see cref="LiveNetworkAdapters"/>; ciclo de vida, rede, tique, guarda e rotas vêm de quem compõe
    /// (specs/005-presentation-facade/research.md, R8). Substitui os dois <c>FromAdapters</c> da conta e das conexões.
    /// </summary>
    /// <example>
    /// <code>
    /// ClientPorts ports = LivePorts.Create(adapters, lifecycle, reachability, ticker, vault, accountRoutes, connectionRoutes);
    /// AccountServices account = new AccountServices(ports);
    /// </code>
    /// </example>
    internal static class LivePorts
    {
        /// <summary>Monta as portas com a margem de renovação padrão.</summary>
        /// <example><code>ClientPorts ports = LivePorts.Create(adapters, lifecycle, reachability, ticker, vault, accountRoutes, connectionRoutes);</code></example>
        public static ClientPorts Create(LiveNetworkAdapters adapters, IAppLifecycle lifecycle, INetworkReachability reachability, IFrameTicker ticker,
            IRefreshTokenVault vault, AccountRoutes accountRoutes, ConnectionRoutes connectionRoutes)
        {
            LiveNetworkAdapters required = adapters ?? throw new ArgumentNullException(nameof(adapters), "adapters are null: expected LiveNetworkAdapters.Create(queue, log, policy)");
            return new ClientPorts(required.Http, required.Sockets, required.Clock, ticker, lifecycle, reachability, required.Queue, required.Log,
                required.Codec, vault, accountRoutes, connectionRoutes, new AccountTiming());
        }
    }
}
