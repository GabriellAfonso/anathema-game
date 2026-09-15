#nullable enable
using System;
using Anathema.Net.Connection;

namespace Anathema.Net.Facade
{
    /// <summary>A conexão ativa não vai mais tentar; o texto é para o jogador ler.</summary>
    /// <example><code>subscriptions.Add(client.Health.GaveUp.Subscribe(notice => label.text = notice.PlayerText));</code></example>
    public sealed class GaveUpNotice
    {
        internal GaveUpNotice(GiveUpReason reason)
        {
            Reason = reason ?? throw new ArgumentNullException(nameof(reason), "give up reason is null: expected the reason from the connection status");
            PlayerText = reason.PlayerText();
        }

        /// <summary>O motivo tipado.</summary>
        /// <example><code>GiveUpKind kind = notice.Reason.Kind;</code></example>
        public GiveUpReason Reason { get; }

        /// <summary>Texto para o jogador.</summary>
        /// <example><code>label.text = notice.PlayerText;</code></example>
        public string PlayerText { get; }
    }
}
