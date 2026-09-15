#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Junta as quatro fontes de sessão perdida numa transição só para o login: o aviso da sessão de conta, a
    /// desistência da fila e da partida por <c>SessionExpired</c>/<c>NoSession</c>, e o catálogo sem sessão. A
    /// desistência da conexão pode chegar antes ou depois do aviso da sessão; as duas ordens dão o mesmo estado
    /// (specs/005-presentation-facade/research.md, R7).
    /// </summary>
    internal sealed class SessionExpiryWatch : IDisposable
    {
        private readonly AccountSession session;
        private readonly MatchQueue queue;
        private readonly ClientStages stages;
        private readonly MainThreadQueue mainThread;
        private readonly Action<SignedOutReason, string> tearDown;

        internal SessionExpiryWatch(AccountSession session, MatchQueue queue, ClientStages stages, MainThreadQueue mainThread, Action<SignedOutReason, string> tearDown)
        {
            this.session = session;
            this.queue = queue;
            this.stages = stages;
            this.mainThread = mainThread;
            this.tearDown = tearDown;
            session.SessionExpired += OnSessionExpired;
            queue.LeftQueue += OnLeftQueue;
        }

        internal static bool IsSessionLoss(GiveUpReason reason) => reason.Kind == GiveUpKind.SessionExpired || reason.Kind == GiveUpKind.NoSession;

        internal bool NoticeGiveUp(GiveUpReason reason)
        {
            if (!IsSessionLoss(reason))
                return false;

            Expire("connection_session_lost");
            return true;
        }

        internal void Expire(string cause)
        {
            if (stages.State.Stage != ClientStage.SignedOut)
                tearDown(SignedOutReason.SessionExpired, cause);
        }

        public void Dispose()
        {
            session.SessionExpired -= OnSessionExpired;
            queue.LeftQueue -= OnLeftQueue;
        }

        // A renovação completa fora da thread principal quando o transporte quiser: a transição espera a drenagem (research R5).
        private void OnSessionExpired() => mainThread.Enqueue(() => Expire("session_expired"));

        private void OnLeftQueue(GiveUpReason reason) => NoticeGiveUp(reason);
    }
}
