#nullable enable
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// O único lugar da camada de rede que escreve no console do Unity (constituição,
    /// "Logging"). Uma linha por registro, que chega ao <c>adb logcat -s Unity</c>.
    /// <c>Debug.Log</c> é seguro de qualquer thread.
    /// </summary>
    /// <example>
    /// <code>
    /// IClientLog log = new UnityConsoleLog();
    /// log.Info("connection_probe_passed", new LogField("host", host));
    /// </code>
    /// </example>
    internal sealed class UnityConsoleLog : IClientLog
    {
        /// <summary>Escreve a entrada no nível correspondente do console.</summary>
        /// <example><code>log.Write(entry);</code></example>
        public void Write(ClientLogEntry entry)
        {
            string line = ClientLogLineFormat.Format(entry);
            switch (entry.Level)
            {
                case ClientLogLevel.Error:
                    Debug.LogError(line);
                    break;
                case ClientLogLevel.Warning:
                    Debug.LogWarning(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }
        }
    }
}
