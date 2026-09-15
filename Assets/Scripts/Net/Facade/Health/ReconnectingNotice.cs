#nullable enable
using System;

namespace Anathema.Net.Facade
{
    /// <summary>A conexão ativa caiu e vai tentar de novo: número da tentativa e quanto falta.</summary>
    /// <example><code>subscriptions.Add(client.Health.Reconnecting.Subscribe(notice => label.text = $"Reconectando... (tentativa {notice.Attempt})"));</code></example>
    public sealed class ReconnectingNotice
    {
        internal ReconnectingNotice(int attempt, TimeSpan wait)
        {
            Attempt = attempt;
            Wait = wait;
        }

        /// <summary>A tentativa; sem rede, repete a da queda que causou a suspensão.</summary>
        /// <example><code>int attempt = notice.Attempt;</code></example>
        public int Attempt { get; }

        /// <summary>Quanto falta para a próxima abertura; zero na suspensão sem rede.</summary>
        /// <example><code>double seconds = notice.Wait.TotalSeconds;</code></example>
        public TimeSpan Wait { get; }
    }
}
