#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>A decisao, com o motivo junto para virar log e mensagem de UI.</summary>
    /// <example><code>ReconnectPlan plan = new ReconnectPlan(ReconnectAction.Retry, 0.5, "close 1006");</code></example>
    public readonly struct ReconnectPlan
    {
        /// <summary>Plano com acao, espera e motivo.</summary>
        /// <example><code>ReconnectPlan plan = new ReconnectPlan(ReconnectAction.GiveUp, 0, "partida nao existe mais");</code></example>
        public ReconnectPlan(ReconnectAction action, double delaySeconds, string reason)
        {
            Action = action;
            DelaySeconds = delaySeconds;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason), $"reconnect plan reason is null for {action}: expected the text that explains the decision");
        }

        /// <summary>O que fazer.</summary>
        /// <example><code>ReconnectAction action = plan.Action;</code></example>
        public ReconnectAction Action { get; }

        /// <summary>Quanto esperar antes da nova tentativa, em segundos.</summary>
        /// <example><code>TimeSpan wait = TimeSpan.FromSeconds(plan.DelaySeconds);</code></example>
        public double DelaySeconds { get; }

        /// <summary>Por que, para log.</summary>
        /// <example><code>string reason = plan.Reason;</code></example>
        public string Reason { get; }

        /// <summary>Resumo legivel no log.</summary>
        /// <example><code>string text = plan.ToString();</code></example>
        public override string ToString()
        {
            return $"{Action} em {DelaySeconds:0.##}s ({Reason})";
        }
    }
}
