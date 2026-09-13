#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Adaptadores reais contra o backend local (<c>docker compose up</c> em
    /// C:/Users/gabri/Projetos/dev_container/anathema/backend). Fora da rodada normal.
    /// </summary>
    [Explicit, Category("LiveServer")]
    public class LiveServerProbeTests
    {
        private const string HttpBase = "http://127.0.0.1:8000";
        private const string WsBase = "ws://127.0.0.1:8000";
        private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(15);

        [UnityTest]
        public IEnumerator HttpSemTokenRecebe401()
        {
            return RunStep(steps => steps.CheckUnauthorizedHttpAsync());
        }

        [UnityTest]
        public IEnumerator SocketComTokenInvalidoRecebeAuthDeniedE4001()
        {
            return RunStep(steps => steps.CheckAuthDeniedAsync());
        }

        [UnityTest]
        public IEnumerator PingComMarcadorVoltaNoPong()
        {
            return RunStep(steps => steps.CheckPingPongAsync());
        }

        private static IEnumerator RunStep(Func<LiveServerProbeSteps, Task<ProbeStepResult>> step)
        {
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            FakeClientLog log = new FakeClientLog();
            MainThreadQueue queue = new MainThreadQueue(log);
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(true));
            Task<ProbeStepResult> running = step(new LiveServerProbeSteps(adapters, HttpBase, WsBase));

            Stopwatch waited = Stopwatch.StartNew();
            while (!running.IsCompleted && waited.Elapsed < StepTimeout)
            {
                queue.Drain();
                yield return null;
            }

            Assert.That(running.IsCompleted, Is.True, $"step did not finish in {StepTimeout.TotalSeconds}s: is the backend up on {HttpBase}?");
            Assert.That(running.Result.Passed, Is.True, running.Result.ToString());
            Assert.That(running.Result.ObservedThreadIds, Is.All.EqualTo(mainThread));
            queue.Close();
        }
    }
}
