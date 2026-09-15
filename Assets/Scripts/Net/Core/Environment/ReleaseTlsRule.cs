#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Build de produção não sai com TLS desligado (FR-046): token trafegaria em texto puro. A
    /// mensagem diz a cena, o config, o ambiente e o que corrigir, porque é lida por quem
    /// apertou Build, não por quem escreveu o gate.
    /// </summary>
    /// <example>
    /// <code>
    /// string? violation = ReleaseTlsRule.Check(false, scene.path, config.name, isProd, config.useTls);
    /// if (violation != null) throw new BuildFailedException(violation);
    /// </code>
    /// </example>
    internal static class ReleaseTlsRule
    {
        /// <summary>Mensagem da violação, ou nulo quando o build pode seguir.</summary>
        /// <example><code>string? violation = ReleaseTlsRule.Check(developmentBuild: false, "Assets/Scenes/BootstrapScene.unity", "AppConfig_Dev", false, false);</code></example>
        public static string? Check(bool developmentBuild, string scenePath, string configName, bool isProd, bool useTls)
        {
            if (developmentBuild || useTls)
                return null;

            return $"Build de produção bloqueado: a cena {scenePath} seleciona o AppConfig '{configName}' " +
                   $"(isProd={Lower(isProd)}) com useTls=false. Um build sem \"Development Build\" exige TLS: " +
                   "marque isProd e use um AppConfig com useTls ligado, ou gere com \"Development Build\".";
        }

        /// <summary>Mensagem para o campo de config selecionado vazio.</summary>
        /// <example><code>string message = ReleaseTlsRule.MissingConfig("Assets/Scenes/BootstrapScene.unity", "configProd");</code></example>
        public static string MissingConfig(string scenePath, string selectedField)
        {
            return $"Build de produção bloqueado: a cena {scenePath} seleciona o campo {selectedField}, que está vazio. " +
                   $"Atribua um AppConfig com useTls ligado em {selectedField}.";
        }

        private static string Lower(bool value) => value ? "true" : "false";
    }
}
