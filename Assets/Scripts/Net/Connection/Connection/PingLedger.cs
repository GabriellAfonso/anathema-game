#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Os pings pendentes de uma conexão, para casar cada pong pelo marcador ecoado e medir a latência
    /// no relógio monotônico. O servidor não acrescenta nada ao eco, e entre um ping e o pong dele
    /// podem chegar outros frames; por isso o casamento é pelo marcador, nunca pela posição (contrato
    /// 013 do backend, <c>specs/013-socket-heartbeat/contracts/heartbeat_messages.md</c>).
    /// </summary>
    internal sealed class PingLedger
    {
        internal const int MaxPending = 8;

        private readonly List<(PingMarker Marker, MonotonicInstant SentAt)> pending = new List<(PingMarker, MonotonicInstant)>();
        private long sequence;

        internal PingMarker Next(MonotonicInstant now)
        {
            sequence++;
            PingMarker marker = new PingMarker(now.Ticks / TimeSpan.TicksPerMillisecond, sequence);
            pending.Add((marker, now));
            if (pending.Count > MaxPending)
                pending.RemoveAt(0);

            return marker;
        }

        internal TimeSpan? Match(PingMarker? echoed, MonotonicInstant now)
        {
            int index = echoed == null ? -1 : pending.FindIndex(entry => entry.Marker.Equals(echoed));
            if (index < 0)
                return null;

            MonotonicInstant sentAt = pending[index].SentAt;
            pending.RemoveAt(index);
            return now - sentAt;
        }

        internal void Clear() => pending.Clear();
    }
}
