#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Monta a camada inteira sobre os adaptadores reais: fila da thread principal, HTTP, socket, relógio, ciclo de
    /// vida, rede, tique, guarda e a fachada. O hospedeiro de cena, a prova no editor e a prova no aparelho montam
    /// pelo mesmo caminho (specs/005-presentation-facade/research.md, R8).
    /// </summary>
    /// <example>
    /// <code>
    /// ComposedClient composed = ClientComposition.Compose(new ClientCompositionOptions(config.BuildServerRoutes(), Debug.isDebugBuild));
    /// composed.AttachTo(gameObject);
    /// </code>
    /// </example>
    public static class ClientComposition
    {
        /// <summary>Compõe a camada com as opções dadas.</summary>
        /// <example><code>ComposedClient composed = ClientComposition.Compose(options);</code></example>
        public static ComposedClient Compose(ClientCompositionOptions options)
        {
            ClientCompositionOptions required = options ?? throw new ArgumentNullException(nameof(options), "composition options are null: expected new ClientCompositionOptions(routes, allowCleartext)");
            IClientLog log = required.Log ?? new UnityConsoleLog();
            MainThreadQueue queue = new MainThreadQueue(log);
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(required.AllowCleartext));
            return new ComposedClient(required, adapters);
        }

        /// <summary>O log de console do Unity, para a borda registrar antes de compor (ex.: configuração ausente).</summary>
        /// <example><code>IClientLog log = ClientComposition.CreateConsoleLog();</code></example>
        public static IClientLog CreateConsoleLog() => new UnityConsoleLog();
    }
}
