#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// A fachada composta e o que a borda precisa para mantê-la andando: o passo de cada quadro, a ligação a um
    /// objeto de cena e, para a prova final, derrubar os sockets e fazer o relógio saltar. A apresentação não
    /// referencia esta assembly e não alcança nada disso (specs/005-presentation-facade/plan.md, Complexity Tracking).
    /// </summary>
    /// <example>
    /// <code>
    /// ComposedClient composed = ClientComposition.Compose(options);
    /// composed.AttachTo(gameObject);
    /// AnathemaClient client = composed.Client;
    /// </code>
    /// </example>
    public sealed class ComposedClient : IDisposable
    {
        private readonly MainThreadQueue queue;
        private readonly UnityAppLifecycle lifecycle;
        private readonly UnityNetworkReachability reachability;
        private readonly UnityFrameTicker ticker = new UnityFrameTicker();
        private readonly SteppableMonotonicClock clock;
        private readonly DroppableWebSocketFactory sockets;
        private readonly bool allowClockJumps;

        internal ComposedClient(ClientCompositionOptions options, LiveNetworkAdapters adapters)
        {
            queue = adapters.Queue;
            clock = new SteppableMonotonicClock(adapters.Clock);
            sockets = new DroppableWebSocketFactory(adapters.Sockets);
            lifecycle = UnityAppLifecycle.Create(clock, queue);
            reachability = UnityNetworkReachability.Create(clock, queue);
            allowClockJumps = options.AllowClockJumps;
            IRefreshTokenVault vault = PlatformRefreshTokenVault.Create(adapters.Log, options.VaultSlot);
            ClientPorts ports = new ClientPorts(adapters.Http, sockets, clock, ticker, lifecycle, reachability, queue, adapters.Log, adapters.Codec, vault,
                options.Routes.Account, options.Routes.Connection, new AccountTiming());
            Client = new AnathemaClient(ports);
        }

        /// <summary>A fachada composta.</summary>
        /// <example><code>AnathemaClient client = composed.Client;</code></example>
        public AnathemaClient Client { get; }

        /// <summary>O passo de um quadro: consulta a rede, drena a fila da thread principal e dá o tique. Chame na thread principal.</summary>
        /// <example><code>while (!running.IsCompleted) { composed.Pump(); yield return null; }</code></example>
        public void Pump() => NetworkLayerHost.Step(reachability, queue, ticker);

        /// <summary>Liga um <see cref="NetworkLayerHost"/> ao objeto, que dá o passo a cada quadro e repassa pausa e foco.</summary>
        /// <example><code>composed.AttachTo(gameObject);</code></example>
        public void AttachTo(GameObject host)
        {
            GameObject required = host != null ? host : throw new ArgumentNullException(nameof(host), "host object is null: expected the GameObject that keeps the layer alive");
            required.AddComponent<NetworkLayerHost>().Attach(Pump, lifecycle, queue);
        }

        /// <summary>Derruba os sockets vivos como uma queda de rede; a conexão reconecta sozinha.</summary>
        /// <example><code>composed.DropSockets();</code></example>
        public void DropSockets() => sockets.DropAll();

        /// <summary>Faz o relógio monotônico da camada saltar para frente; só com <see cref="ClientCompositionOptions.AllowClockJumps"/>.</summary>
        /// <example><code>composed.JumpClock(TimeSpan.FromMinutes(5.5));</code></example>
        public void JumpClock(TimeSpan forward)
        {
            if (!allowClockJumps)
                throw new InvalidOperationException($"clock jump of {forward.TotalSeconds} s refused: expected a composition with ClientCompositionOptions.AllowClockJumps set");

            clock.Jump(forward);
        }

        /// <summary>Fecha fila e partida de propósito e a fila da thread principal.</summary>
        /// <example><code>composed.Dispose();</code></example>
        public void Dispose()
        {
            Client.Dispose();
            queue.Close();
        }
    }
}
