#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// O que quem compõe escolhe: rotas do servidor, tráfego sem TLS, slot da guarda, log e se o relógio pode saltar.
    /// O hospedeiro de cena e a prova final usam a mesma composição com opções diferentes
    /// (specs/005-presentation-facade/contracts/composition-and-scenes.md, FR-025).
    /// </summary>
    /// <example>
    /// <code>
    /// ClientCompositionOptions options = new ClientCompositionOptions(config.BuildServerRoutes(), Debug.isDebugBuild);
    /// ComposedClient composed = ClientComposition.Compose(options);
    /// </code>
    /// </example>
    public sealed class ClientCompositionOptions
    {
        /// <summary>Opções com as rotas e a política de cleartext; o resto começa no padrão.</summary>
        /// <example><code>ClientCompositionOptions options = new ClientCompositionOptions(routes, allowCleartext: Debug.isDebugBuild);</code></example>
        public ClientCompositionOptions(ServerRoutes routes, bool allowCleartext)
        {
            Routes = routes ?? throw new ArgumentNullException(nameof(routes), "server routes are null: expected AppConfig.BuildServerRoutes() or ServerRoutes.ForHost");
            AllowCleartext = allowCleartext;
        }

        /// <summary>As rotas do servidor.</summary>
        /// <example><code>ServerRoutes routes = options.Routes;</code></example>
        public ServerRoutes Routes { get; }

        /// <summary>Libera <c>http://</c> e <c>ws://</c>; só em build de desenvolvimento.</summary>
        /// <example><code>bool development = options.AllowCleartext;</code></example>
        public bool AllowCleartext { get; }

        /// <summary>O slot da guarda do refresh token; nulo usa o da plataforma (jogador ou projeto do editor).</summary>
        /// <example><code>ClientCompositionOptions options = new ClientCompositionOptions(routes, true) { VaultSlot = RefreshTokenVaultSlot.Named("proof-p1") };</code></example>
        public RefreshTokenVaultSlot? VaultSlot { get; set; }

        /// <summary>O log da camada; nulo usa o console do Unity.</summary>
        /// <example><code>ClientCompositionOptions options = new ClientCompositionOptions(routes, true) { Log = proofLog };</code></example>
        public IClientLog? Log { get; set; }

        /// <summary>Permite <see cref="ComposedClient.JumpClock"/>; fica desligado fora da prova.</summary>
        /// <example><code>ClientCompositionOptions options = new ClientCompositionOptions(routes, true) { AllowClockJumps = true };</code></example>
        public bool AllowClockJumps { get; set; }
    }
}
