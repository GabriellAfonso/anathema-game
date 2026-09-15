#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>A conexão ativa voltou depois de ter caído; o overlay pode sumir.</summary>
    /// <example><code>subscriptions.Add(client.Health.Recovered.Subscribe(_ => panel.SetActive(false)));</code></example>
    public sealed class RecoveredNotice
    {
        internal RecoveredNotice()
        {
        }
    }
}
