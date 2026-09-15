#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Resultado de um passo do probe: se passou, os campos que ele registrou e em quais
    /// threads os eventos observados chegaram (SC-004).
    /// </summary>
    /// <example>
    /// <code>
    /// ProbeStepResult result = await steps.CheckPingPongAsync();
    /// if (!result.Passed) log.Error("connection_probe_failed", new LogField("step", result.StepName));
    /// </code>
    /// </example>
    internal sealed class ProbeStepResult
    {
        /// <summary>Cria o resultado.</summary>
        /// <example><code>ProbeStepResult result = new ProbeStepResult("ping_pong", true, fields, threadIds);</code></example>
        public ProbeStepResult(string stepName, bool passed, IReadOnlyList<LogField> fields, IReadOnlyCollection<int> observedThreadIds)
        {
            StepName = stepName;
            Passed = passed;
            Fields = fields.ToArray();
            ObservedThreadIds = observedThreadIds.ToArray();
        }

        /// <summary>Nome estável do passo.</summary>
        /// <example><code>string step = result.StepName; // ping_pong</code></example>
        public string StepName { get; }

        /// <summary>Verdadeiro quando o servidor respondeu como o contrato diz.</summary>
        /// <example><code>Assert.That(result.Passed, result.ToString());</code></example>
        public bool Passed { get; }

        /// <summary>Campos registrados pelo passo.</summary>
        /// <example><code>LogField status = result.Fields[0];</code></example>
        public IReadOnlyList<LogField> Fields { get; }

        /// <summary>Threads em que os eventos observados chegaram ao assinante.</summary>
        /// <example><code>Assert.That(result.ObservedThreadIds, Is.All.EqualTo(mainThreadId));</code></example>
        public IReadOnlyCollection<int> ObservedThreadIds { get; }

        /// <summary>Forma para log e mensagem de teste.</summary>
        /// <example><code>string text = result.ToString(); // ping_pong passed=true marker_matched=true</code></example>
        public override string ToString()
        {
            return StepName + " passed=" + (Passed ? "true" : "false") + string.Concat(Fields.Select(field => " " + field.Name + "=" + field.Value));
        }
    }
}
