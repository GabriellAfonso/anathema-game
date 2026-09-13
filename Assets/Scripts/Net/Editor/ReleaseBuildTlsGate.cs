#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anathema.Net.Editor
{
    /// <summary>
    /// Falha o build de produção, antes de gerar o pacote, se a cena seleciona um
    /// <see cref="AppConfig"/> com TLS desligado (FR-046). Roda por cena porque é a cena que diz
    /// qual config vale (specs/001-server-connection/research.md, R6).
    /// </summary>
    /// <example>
    /// <code>
    /// // registrado pelo Unity; em teste:
    /// ReleaseBuildTlsGate.Validate("Assets/Scenes/BootstrapScene.unity", components);
    /// </code>
    /// </example>
    public sealed class ReleaseBuildTlsGate : IProcessSceneWithReport
    {
        /// <summary>Ordem entre os processadores de cena.</summary>
        /// <example><code>int order = new ReleaseBuildTlsGate().callbackOrder;</code></example>
        public int callbackOrder => 0;

        /// <summary>Chamado pelo Unity para cada cena do build (e ao entrar no Play Mode, sem relatório).</summary>
        /// <example><code>gate.OnProcessScene(scene, report);</code></example>
        public void OnProcessScene(Scene scene, BuildReport? report)
        {
            BuildOptions options = report == null ? BuildOptions.None : report.summary.options;
            if (ShouldValidate(report != null, options))
                Validate(scene.path, SceneComponents(scene));
        }

        /// <summary>Só build de verdade (com relatório) e sem Development Build é validado.</summary>
        /// <example><code>bool validate = ReleaseBuildTlsGate.ShouldValidate(true, BuildOptions.None);</code></example>
        public static bool ShouldValidate(bool hasReport, BuildOptions options)
        {
            return hasReport && (options & BuildOptions.Development) == 0;
        }

        /// <summary>Lança <see cref="BuildFailedException"/> no primeiro seletor que escolhe config sem TLS.</summary>
        /// <example><code>ReleaseBuildTlsGate.Validate(scene.path, new Component[] { environmentManager });</code></example>
        public static void Validate(string scenePath, IEnumerable<Component> components)
        {
            foreach (Component component in components)
            {
                string? violation = Violation(scenePath, component);
                if (violation != null)
                    throw new BuildFailedException(violation);
            }
        }

        private static string? Violation(string scenePath, Component component)
        {
            if (!EnvironmentSelectionReader.TryRead(component, out EnvironmentSelection? selection))
                return null;

            if (selection.Config == null)
                return ReleaseTlsRule.MissingConfig(scenePath, selection.SelectedField);

            return ReleaseTlsRule.Check(false, scenePath, selection.Config.name, selection.IsProd, selection.Config.useTls);
        }

        private static IEnumerable<Component> SceneComponents(Scene scene)
        {
            // Componente com script ausente vem nulo e não serializa.
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(component => component != null);
        }
    }
}
