#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// O log de um cliente da prova: guarda as entradas em ordem, para o roteiro conferir o que a camada registrou depois
    /// de uma marca (a renovação antes da reabertura, no passo 12), e repassa cada uma ao log de console. Thread-safe, porque
    /// a camada registra de fora da thread principal.
    /// </summary>
    internal sealed class ProofLog : IClientLog
    {
        private readonly object gate = new object();
        private readonly List<ClientLogEntry> entries = new List<ClientLogEntry>();
        private readonly IClientLog console;

        internal ProofLog(IClientLog console)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console), "console log is null: expected the log each entry is forwarded to");
        }

        internal IReadOnlyList<ClientLogEntry> Entries
        {
            get
            {
                lock (gate)
                    return entries.ToArray();
            }
        }

        /// <summary>Guarda a entrada e a repassa ao console.</summary>
        /// <example><code>log.Write(entry);</code></example>
        public void Write(ClientLogEntry entry)
        {
            lock (gate)
                entries.Add(entry);

            console.Write(entry);
        }

        internal int Mark(string eventName, params LogField[] fields)
        {
            ClientLogEntry mark = new ClientLogEntry(ClientLogLevel.Info, eventName, fields);
            int index;
            lock (gate)
            {
                entries.Add(mark);
                index = entries.Count - 1;
            }

            console.Write(mark);
            return index;
        }

        internal int IndexOfFirst(string eventName, int after)
        {
            IReadOnlyList<ClientLogEntry> copy = Entries;
            for (int index = after + 1; index < copy.Count; index++)
            {
                if (copy[index].EventName == eventName)
                    return index;
            }

            return -1;
        }
    }
}
