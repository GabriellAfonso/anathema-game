#nullable enable
using System;

namespace Anathema.Net.Match
{
    /// <summary>
    /// Uma jogada recusada pelo servidor, tipada pelo código. O servidor não ecoa a mensagem recusada:
    /// <see cref="ProbableCommand"/> é o último comando enviado desde a última atualização aceita, por melhor
    /// esforço, e pode apontar o comando errado quando dois saem seguidos.
    /// </summary>
    /// <example>
    /// <code>
    /// match.Refused.Subscribe(refusal => log.Warning("play_refused", new LogField("code", refusal.CodeText)));
    /// </code>
    /// </example>
    public sealed class PlayRefusal
    {
        /// <summary>Recusa com código, texto do código, texto do servidor e comando provável.</summary>
        /// <example><code>PlayRefusal refusal = new PlayRefusal(PlayRefusalCode.NotEnoughEnergy, "not_enough_energy", "costs 5", command);</code></example>
        public PlayRefusal(PlayRefusalCode code, string codeText, string error, PlayCommand? probableCommand)
        {
            Code = code;
            CodeText = codeText ?? throw new ArgumentNullException(nameof(codeText), $"refusal code text of {code} is null: expected the code as sent");
            Error = error ?? throw new ArgumentNullException(nameof(error), $"refusal error of '{codeText}' is null: expected the server error text");
            ProbableCommand = probableCommand;
        }

        /// <summary>O código, para decidir o que mostrar.</summary>
        /// <example><code>bool energy = refusal.Code == PlayRefusalCode.NotEnoughEnergy;</code></example>
        public PlayRefusalCode Code { get; }

        /// <summary>O texto de <c>code</c> como veio.</summary>
        /// <example><code>string raw = refusal.CodeText;</code></example>
        public string CodeText { get; }

        /// <summary>O texto de <c>error</c>, só para log: pode mudar e nunca é comparado.</summary>
        /// <example><code>log.Debug("refused", new LogField("error", refusal.Error));</code></example>
        public string Error { get; }

        /// <summary>O último comando enviado antes da recusa, por melhor esforço; nulo se nada foi enviado.</summary>
        /// <example><code>string command = refusal.ProbableCommand?.MessageType ?? "none";</code></example>
        public PlayCommand? ProbableCommand { get; }
    }
}
