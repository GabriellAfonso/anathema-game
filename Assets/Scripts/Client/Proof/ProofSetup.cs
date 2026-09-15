#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Unity;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// O que muda entre a prova no editor e a prova no aparelho: rotas, log de console, quem liga os clientes a um objeto de
    /// cena, e os números do roteiro (specs/005-presentation-facade/data-model.md, "Prova").
    /// </summary>
    /// <example>
    /// <code>
    /// ProofSetup setup = new ProofSetup(LocalServerRoutes.Create(), ClientComposition.CreateConsoleLog());
    /// MatchProofScript script = new MatchProofScript(setup);
    /// </code>
    /// </example>
    public sealed class ProofSetup
    {
        /// <summary>Configuração com rotas e log; o resto começa no padrão do contrato.</summary>
        /// <example><code>ProofSetup setup = new ProofSetup(config.BuildServerRoutes(), log) { Attach = AttachHost };</code></example>
        public ProofSetup(ServerRoutes routes, IClientLog consoleLog)
        {
            Routes = routes ?? throw new ArgumentNullException(nameof(routes), "server routes are null: expected LocalServerRoutes.Create() or AppConfig.BuildServerRoutes()");
            ConsoleLog = consoleLog ?? throw new ArgumentNullException(nameof(consoleLog), "console log is null: expected ClientComposition.CreateConsoleLog()");
        }

        /// <summary>As rotas do servidor.</summary>
        /// <example><code>ServerRoutes routes = setup.Routes;</code></example>
        public ServerRoutes Routes { get; }

        /// <summary>Onde saem <c>proof_step</c>, <c>proof_coverage</c> e <c>proof_finished</c>, e para onde o log de cada cliente repassa.</summary>
        /// <example><code>IClientLog log = setup.ConsoleLog;</code></example>
        public IClientLog ConsoleLog { get; }

        /// <summary>Libera <c>http://</c> e <c>ws://</c>; ligado por padrão, porque a prova roda contra o backend local.</summary>
        /// <example><code>setup.AllowCleartext = Debug.isDebugBuild;</code></example>
        public bool AllowCleartext { get; set; } = true;

        /// <summary>Sufixo dos nomes de conta e dos slots da guarda, único por execução.</summary>
        /// <example><code>string suffix = setup.Suffix;</code></example>
        public string Suffix { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Tempo limite do roteiro inteiro.</summary>
        /// <example><code>setup.Timeout = TimeSpan.FromMinutes(15);</code></example>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>Rodada a partir da qual P2 derruba os sockets (passo 11).</summary>
        /// <example><code>setup.DropRound = 3;</code></example>
        public long DropRound { get; set; } = 3;

        /// <summary>Rodada a partir da qual P2 deixa a primeira vez de ação estourar (passo 10).</summary>
        /// <example><code>setup.StallFromRound = 2;</code></example>
        public long StallFromRound { get; set; } = 2;

        /// <summary>Salto do relógio de P2, acima dos 5 min do token menos a margem de 30 s (passo 12).</summary>
        /// <example><code>setup.ClockJump = TimeSpan.FromSeconds(330);</code></example>
        public TimeSpan ClockJump { get; set; } = TimeSpan.FromSeconds(330);

        /// <summary>
        /// Chamado a cada composição com o rótulo do jogador; no aparelho liga o cliente a um objeto de cena. Nulo no teste,
        /// que bombeia pelo <see cref="MatchProofScript.Pump"/>.
        /// </summary>
        /// <example><code>setup.Attach = (label, composed) => composed.AttachTo(new GameObject("Proof " + label));</code></example>
        public Action<string, ComposedClient>? Attach { get; set; }
    }
}
