#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Tipo de rede disponível. Não prova que o servidor responde: Wi-Fi com portal
    /// cativo aparece como <see cref="LocalArea"/>.
    /// </summary>
    /// <example><code>if (reachability.Current == NetworkKind.None) ShowOffline();</code></example>
    internal enum NetworkKind
    {
        /// <summary>Sem rede.</summary>
        None,

        /// <summary>Wi-Fi ou cabo.</summary>
        LocalArea,

        /// <summary>Dados móveis.</summary>
        CarrierData,
    }
}
