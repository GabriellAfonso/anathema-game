#nullable enable

namespace Anathema.Net.Editor
{
    /// <summary>Qual <see cref="AppConfig"/> um seletor de ambiente da cena escolhe.</summary>
    /// <example>
    /// <code>
    /// if (EnvironmentSelectionReader.TryRead(component, out EnvironmentSelection? selection) &amp;&amp; selection.Config == null) Fail(selection.SelectedField);
    /// </code>
    /// </example>
    public sealed class EnvironmentSelection
    {
        /// <summary>Cria a seleção lida.</summary>
        /// <example><code>EnvironmentSelection selection = new EnvironmentSelection(false, "configDev", devConfig);</code></example>
        public EnvironmentSelection(bool isProd, string selectedField, AppConfig? config)
        {
            IsProd = isProd;
            SelectedField = selectedField;
            Config = config;
        }

        /// <summary>Valor de <c>isProd</c> no seletor.</summary>
        /// <example><code>bool production = selection.IsProd;</code></example>
        public bool IsProd { get; }

        /// <summary>Nome do campo escolhido: <c>configProd</c> ou <c>configDev</c>.</summary>
        /// <example><code>string field = selection.SelectedField;</code></example>
        public string SelectedField { get; }

        /// <summary>O config atribuído nesse campo, ou nulo se vazio.</summary>
        /// <example><code>bool tls = selection.Config != null &amp;&amp; selection.Config.useTls;</code></example>
        public AppConfig? Config { get; }
    }
}
