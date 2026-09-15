#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Unity;
using UnityEngine;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// A prova no aparelho: na <c>MatchProofScene</c> de um build de desenvolvimento, lê o <c>AppConfig</c> (com o host de
    /// lançamento <c>serverHost</c>), liga cada cliente a um objeto filho, que repassa pausa e foco, e roda o roteiro. O
    /// resultado sai no <c>adb logcat -s Unity</c> como <c>proof_step</c> e <c>proof_finished</c>
    /// (specs/005-presentation-facade/quickstart.md, §5).
    /// </summary>
    /// <example><code>// Componente do único objeto da Assets/Scenes/Dev/MatchProofScene.unity, com o AppConfig_Dev no Inspector.</code></example>
    public sealed class MatchProofRunner : MonoBehaviour
    {
        // Preenchido pelo Inspector na MatchProofScene; o valor inicial só cala o aviso de campo nunca atribuído.
        [SerializeField] private AppConfig? config = null;

        private readonly Dictionary<string, GameObject> hosts = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private MatchProofScript? script;

        private void Start()
        {
            IClientLog log = ClientComposition.CreateConsoleLog();
            if (config == null)
            {
                log.Error("proof_config_missing", new LogField("field", "config"), new LogField("expected", "the development AppConfig asset"));
                return;
            }

            ProofSetup setup = new ProofSetup(config.BuildServerRoutes(), log) { AllowCleartext = Debug.isDebugBuild, Attach = AttachHost };
            script = new MatchProofScript(setup);
            _ = RunAsync(script, log);
        }

        private static async Task RunAsync(MatchProofScript running, IClientLog log)
        {
            try
            {
                await running.RunAsync();
            }
            catch (Exception failure)
            {
                // O roteiro já escreveu proof_step e proof_finished; aqui só não deixa a exceção sumir numa tarefa sem dono.
                log.Error("proof_failed", new LogField("reason", failure.Message));
            }
        }

        private void AttachHost(string label, ComposedClient composed)
        {
            // Recompor (passo 2) troca o cliente: o objeto antigo sai para o passo do quadro não seguir no cliente descartado.
            if (hosts.TryGetValue(label, out GameObject? previous))
                Destroy(previous);

            GameObject host = new GameObject("Proof " + label);
            host.transform.SetParent(transform);
            composed.AttachTo(host);
            hosts[label] = host;
        }

        private void OnDestroy() => script?.Dispose();
    }
}
