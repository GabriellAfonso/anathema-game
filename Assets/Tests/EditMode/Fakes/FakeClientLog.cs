#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Log que guarda as entradas para o teste inspecionar. Thread-safe, porque a
    /// fila da thread principal registra falhas vindas de qualquer thread.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeClientLog log = new FakeClientLog();
    /// ClientLogEntry failure = log.Single("main_thread_item_failed");
    /// </code>
    /// </example>
    internal sealed class FakeClientLog : IClientLog
    {
        private readonly object gate = new object();
        private readonly List<ClientLogEntry> entries = new List<ClientLogEntry>();

        /// <summary>Cópia das entradas, na ordem em que chegaram.</summary>
        /// <example><code>int count = log.Entries.Count;</code></example>
        public IReadOnlyList<ClientLogEntry> Entries
        {
            get
            {
                lock (gate)
                    return entries.ToArray();
            }
        }

        /// <summary>Guarda a entrada.</summary>
        /// <example><code>log.Write(entry);</code></example>
        public void Write(ClientLogEntry entry)
        {
            lock (gate)
                entries.Add(entry);
        }

        /// <summary>A única entrada com esse nome de evento; zero ou mais de uma lança.</summary>
        /// <example><code>ClientLogEntry closed = log.Single("socket_closed");</code></example>
        public ClientLogEntry Single(string eventName)
        {
            ClientLogEntry[] matches = Entries.Where(entry => entry.EventName == eventName).ToArray();
            if (matches.Length == 1)
                return matches[0];

            string seen = string.Join(", ", Entries.Select(entry => entry.EventName));
            throw new InvalidOperationException($"expected exactly 1 log entry '{eventName}', found {matches.Length}; events logged: [{seen}]");
        }
    }
}
