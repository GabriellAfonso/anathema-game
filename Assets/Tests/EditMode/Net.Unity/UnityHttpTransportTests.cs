#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class UnityHttpTransportTests
    {
        [Test]
        public void ChamadoDeOutraThreadCompletaNaThreadQueEsvaziaAFila()
        {
            MainThreadQueue queue = new MainThreadQueue(new FakeClientLog());
            UnityHttpTransport http = new UnityHttpTransport(queue, new CleartextPolicy(false));
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            int continuationThread = -1;

            Task<HttpOutcome> outcome = SendFromAnotherThread(http, new HttpRequestSpec("GET", new Uri("http://127.0.0.1:8000/game/cards/")));
            outcome.ContinueWith(_ => continuationThread = Thread.CurrentThread.ManagedThreadId, TaskContinuationOptions.ExecuteSynchronously);
            Assert.That(outcome.IsCompleted, Is.False);
            queue.Drain();

            Assert.That(outcome.Result.AsFailure!.Kind, Is.EqualTo(TransportFailureKind.CleartextRefused));
            Assert.That(continuationThread, Is.EqualTo(mainThread));
        }

        private static Task<HttpOutcome> SendFromAnotherThread(UnityHttpTransport http, HttpRequestSpec request)
        {
            Task<HttpOutcome>? outcome = null;
            Thread sender = new Thread(() => outcome = http.SendAsync(request));
            sender.Start();
            sender.Join();
            return outcome!;
        }
    }
}
