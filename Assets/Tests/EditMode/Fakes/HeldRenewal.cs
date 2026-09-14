#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Uma renovação que o <see cref="FakeAccessTokenSource"/> segurou. Serve para provar que um
    /// resultado que chega tarde (depois de sair, de trocar de rede) é descartado.
    /// </summary>
    /// <example>
    /// <code>
    /// HeldRenewal held = tokens.HoldNextRenewal();
    /// connection.Leave();
    /// held.Release();
    /// </code>
    /// </example>
    public sealed class HeldRenewal
    {
        private readonly TaskCompletionSource<RenewalOutcome> completion = new TaskCompletionSource<RenewalOutcome>();
        private RenewalOutcome? scripted;

        /// <summary>A renovação foi pedida e ainda não foi solta.</summary>
        /// <example><code>Assert.That(held.IsPending, Is.True);</code></example>
        public bool IsPending => scripted != null && !completion.Task.IsCompleted;

        /// <summary>Completa a renovação com o desfecho roteirizado, na thread do teste.</summary>
        /// <example><code>held.Release();</code></example>
        public void Release()
        {
            if (scripted == null)
                throw new InvalidOperationException("HeldRenewal.Release before RenewNowAsync was called: expected the code under test to request the renewal first");

            completion.TrySetResult(scripted);
        }

        internal Task<RenewalOutcome> Attach(RenewalOutcome outcome)
        {
            scripted = outcome;
            return completion.Task;
        }
    }
}
