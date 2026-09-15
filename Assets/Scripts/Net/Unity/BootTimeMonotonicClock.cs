#nullable enable
using System;
using System.Runtime.InteropServices;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Relógio do Android sobre <c>clock_gettime(CLOCK_BOOTTIME)</c>. O <c>Stopwatch</c> do
    /// IL2CPP lê <c>CLOCK_MONOTONIC</c>, que para com o aparelho dormindo
    /// (il2cpp/libil2cpp/os/Posix/Time.cpp; dotnet/runtime#77945): o tempo fora sairia menor
    /// que o real e o token de 5 minutos venceria sem aviso. Se a chamada falhar, passa a usar
    /// <c>Stopwatch</c> de vez e registra uma vez.
    /// </summary>
    /// <example>
    /// <code>
    /// IMonotonicClock clock = new BootTimeMonotonicClock(log);
    /// </code>
    /// </example>
    internal sealed class BootTimeMonotonicClock : IMonotonicClock
    {
        private const int ClockBootTime = 7;
        private const long NanosecondsPerTick = 100;

        private readonly StopwatchMonotonicClock fallback = new StopwatchMonotonicClock();
        private readonly IClientLog log;
        private volatile bool useFallback;

        /// <summary>Cria o relógio; o log recebe a troca para o fallback.</summary>
        /// <example><code>IMonotonicClock clock = new BootTimeMonotonicClock(log);</code></example>
        public BootTimeMonotonicClock(IClientLog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        }

        /// <summary>Instante atual, contando o tempo de sono do aparelho.</summary>
        /// <example><code>MonotonicInstant now = clock.Now;</code></example>
        public MonotonicInstant Now
        {
            get
            {
                if (!useFallback && TryReadBootTime(out long ticks))
                    return new MonotonicInstant(ticks);

                SwitchToFallback();
                return fallback.Now;
            }
        }

        private static bool TryReadBootTime(out long ticks)
        {
            ticks = 0;
            try
            {
                if (ClockGetTime(ClockBootTime, out Timespec time) != 0)
                    return false;

                ticks = (long)time.Seconds * TimeSpan.TicksPerSecond + (long)time.Nanoseconds / NanosecondsPerTick;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SwitchToFallback()
        {
            if (useFallback)
                return;

            // Uma vez no fallback, fica nele: misturar as duas fontes quebraria a monotonicidade.
            useFallback = true;
            log.Warning("clock_boottime_unavailable");
        }

        [DllImport("libc", EntryPoint = "clock_gettime")]
        private static extern int ClockGetTime(int clockId, out Timespec time);

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public nint Seconds;
            public nint Nanoseconds;
        }
    }
}
