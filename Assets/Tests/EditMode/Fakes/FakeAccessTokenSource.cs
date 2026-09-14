#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Porta de token roteirizada. Cada <c>Enqueue*</c> decide o próximo desfecho de
    /// <see cref="GetValidAsync"/> ou <see cref="RenewNowAsync"/>; sem roteiro, o pedido de token repete o
    /// último válido entregue (como a sessão real) e a renovação lança, para o teste dizer o que espera.
    /// Cria <see cref="AccessToken"/> pelo construtor interno
    /// (specs/003-authenticated-socket-queue/research.md, R5).
    /// </summary>
    /// <example>
    /// <code>
    /// tokens.EnqueueValid("A");
    /// tokens.EnqueueRenewed("B");
    /// connection.Connect(target);
    /// </code>
    /// </example>
    public sealed class FakeAccessTokenSource : IAccessTokenSource
    {
        private static readonly UserId Owner = new UserId(1);
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

        private readonly IMonotonicClock clock;
        private readonly Queue<AccessTokenOutcome> validOutcomes = new Queue<AccessTokenOutcome>();
        private readonly Queue<RenewalOutcome> renewalOutcomes = new Queue<RenewalOutcome>();
        private HeldRenewal? pendingHold;
        private AccessToken? current;
        private SessionUnavailableKind? sessionGone;

        /// <summary>Fonte cujos tokens chegam no instante do relógio dado.</summary>
        /// <example><code>FakeAccessTokenSource tokens = new FakeAccessTokenSource(clock);</code></example>
        public FakeAccessTokenSource(IMonotonicClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock), "clock is null: expected the monotonic clock that stamps token arrival");
        }

        /// <summary>Quantas vezes <see cref="GetValidAsync"/> foi chamado.</summary>
        /// <example><code>Assert.That(tokens.ValidRequests, Is.EqualTo(2));</code></example>
        public int ValidRequests { get; private set; }

        /// <summary>Quantas vezes <see cref="RenewNowAsync"/> foi chamado.</summary>
        /// <example><code>Assert.That(tokens.RenewRequests, Is.EqualTo(1));</code></example>
        public int RenewRequests { get; private set; }

        /// <summary>O próximo pedido de token recebe um token válido com este texto.</summary>
        /// <example><code>tokens.EnqueueValid("A");</code></example>
        public void EnqueueValid(string text) => validOutcomes.Enqueue(AccessTokenOutcome.Valid(Token(text)));

        /// <summary>O próximo pedido de token responde sessão indisponível.</summary>
        /// <example><code>tokens.EnqueueSessionUnavailable(SessionUnavailableKind.NoSession);</code></example>
        public void EnqueueSessionUnavailable(SessionUnavailableKind kind) => validOutcomes.Enqueue(AccessTokenOutcome.SessionUnavailable(kind));

        /// <summary>O próximo pedido de token não sai (renovação sem rede, status ou resposta fora do contrato).</summary>
        /// <example><code>tokens.EnqueueUnavailable(RenewalUnavailableReason.Transport);</code></example>
        public void EnqueueUnavailable(RenewalUnavailableReason reason) => validOutcomes.Enqueue(AccessTokenOutcome.Unavailable(Unavailable(reason)));

        /// <summary>A próxima renovação devolve um token com este texto.</summary>
        /// <example><code>tokens.EnqueueRenewed("B");</code></example>
        public void EnqueueRenewed(string text) => renewalOutcomes.Enqueue(RenewalOutcome.Renewed(Token(text)));

        /// <summary>A próxima renovação expira a sessão.</summary>
        /// <example><code>tokens.EnqueueRenewalExpired();</code></example>
        public void EnqueueRenewalExpired() => renewalOutcomes.Enqueue(RenewalOutcome.SessionExpired());

        /// <summary>A próxima renovação responde que não há sessão.</summary>
        /// <example><code>tokens.EnqueueRenewalNoSession();</code></example>
        public void EnqueueRenewalNoSession() => renewalOutcomes.Enqueue(RenewalOutcome.NoSession());

        /// <summary>A próxima renovação não sai, pelo motivo dado.</summary>
        /// <example><code>tokens.EnqueueRenewalUnavailable(RenewalUnavailableReason.Transport);</code></example>
        public void EnqueueRenewalUnavailable(RenewalUnavailableReason reason) => renewalOutcomes.Enqueue(Unavailable(reason));

        /// <summary>A próxima renovação só completa quando o teste chamar <see cref="HeldRenewal.Release"/>.</summary>
        /// <example><code>HeldRenewal held = tokens.HoldNextRenewal();</code></example>
        public HeldRenewal HoldNextRenewal()
        {
            pendingHold = new HeldRenewal();
            return pendingHold;
        }

        /// <inheritdoc />
        public Task<AccessTokenOutcome> GetValidAsync()
        {
            ValidRequests++;
            AccessTokenOutcome outcome = NextValid();
            if (outcome.Kind == AccessTokenOutcomeKind.Valid)
                current = outcome.Token;

            return Task.FromResult(outcome);
        }

        /// <inheritdoc />
        public Task<RenewalOutcome> RenewNowAsync()
        {
            RenewRequests++;
            if (renewalOutcomes.Count == 0)
                throw new InvalidOperationException("FakeAccessTokenSource.RenewNowAsync with no scripted renewal: expected EnqueueRenewed(text) or another EnqueueRenewal* before the call");

            RenewalOutcome outcome = renewalOutcomes.Dequeue();
            Remember(outcome);
            return Deliver(outcome);
        }

        private AccessTokenOutcome NextValid()
        {
            if (validOutcomes.Count > 0)
                return validOutcomes.Dequeue();

            if (sessionGone.HasValue)
                return AccessTokenOutcome.SessionUnavailable(sessionGone.Value);

            if (current != null)
                return AccessTokenOutcome.Valid(current);

            throw new InvalidOperationException("FakeAccessTokenSource.GetValidAsync with no scripted outcome and no token handed out yet: expected EnqueueValid(text) or another Enqueue* before the call");
        }

        private void Remember(RenewalOutcome outcome)
        {
            if (outcome.Kind == RenewalOutcomeKind.Renewed)
                current = outcome.Token;
            else if (outcome.Kind == RenewalOutcomeKind.SessionExpired)
                sessionGone = SessionUnavailableKind.Expired;
            else if (outcome.Kind == RenewalOutcomeKind.NoSession)
                sessionGone = SessionUnavailableKind.NoSession;
        }

        private Task<RenewalOutcome> Deliver(RenewalOutcome outcome)
        {
            HeldRenewal? hold = pendingHold;
            pendingHold = null;
            return hold == null ? Task.FromResult(outcome) : hold.Attach(outcome);
        }

        private AccessToken Token(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException($"fake token text is '{text}': expected a non-empty text", nameof(text));

            return new AccessToken(text, Owner, Lifetime, clock.Now);
        }

        private static RenewalOutcome Unavailable(RenewalUnavailableReason reason)
        {
            return reason switch
            {
                RenewalUnavailableReason.Transport => RenewalOutcome.TransportFailed(new TransportFailure(TransportFailureKind.CannotConnect, "fake transport failure")),
                RenewalUnavailableReason.ServerStatus => RenewalOutcome.ServerStatus(503),
                RenewalUnavailableReason.OutOfContract => RenewalOutcome.OutOfContract(new DecodeFailure(DecodeFailureKind.InvalidValue, "", "fake out of contract")),
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, $"renewal unavailable reason is {reason}: expected Transport, ServerStatus or OutOfContract"),
            };
        }
    }
}
