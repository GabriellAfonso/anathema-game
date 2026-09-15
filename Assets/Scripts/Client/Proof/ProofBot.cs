#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Um bot headless da prova: a cada estado substituído, recusa ou volta a ao vivo, pede à estratégia o próximo
    /// comando e o manda pela partida corrente da fachada. Guarda envios, recusas, a primeira falha da estratégia, os avisos
    /// de tempo acabando e se viu uma vez estourada seguida de passar no mesmo match_update; confirma a cobertura pelos
    /// eventos que chegam depois do envio (specs/005-presentation-facade/contracts/match-proof.md, "Bots").
    /// </summary>
    internal sealed class ProofBot : IDisposable
    {
        private readonly LiveMatch match;
        private readonly CommandCoverage coverage;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private int updates;
        private (UserId User, int Update)? lastTimeout;

        internal ProofBot(string label, LiveMatch match, ProofStrategy strategy, CommandCoverage coverage)
        {
            Label = label;
            Strategy = strategy;
            this.match = match;
            this.coverage = coverage;
            subscriptions.Add(match.Mirror.ViewReplaced.Subscribe(OnViewReplaced));
            subscriptions.Add(match.Mirror.EventReceived.Subscribe(OnEvent));
            subscriptions.Add(match.Refused.Subscribe(OnRefused));
            subscriptions.Add(match.StatusChanged.Subscribe(OnStatus));
            subscriptions.Add(match.Clock.TurnRunningOut.Subscribe(RunningOutTurns.Add));
            // A partida pode ter recebido o match_start antes de o bot existir: sem isto, ninguém age até o próximo frame.
            Act();
        }

        internal string Label { get; }

        internal ProofStrategy Strategy { get; }

        internal List<PlayRefusal> Refusals { get; } = new List<PlayRefusal>();

        internal Dictionary<string, int> Sent { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        internal Exception? Failure { get; private set; }

        internal List<long> RunningOutTurns { get; } = new List<long>();

        internal List<long> TimedOutTurns { get; } = new List<long>();

        internal bool SawTimedOutPass { get; private set; }

        public void Dispose()
        {
            foreach (IDisposable subscription in subscriptions)
                subscription.Dispose();

            subscriptions.Clear();
        }

        private void OnViewReplaced(ViewReplaced change)
        {
            // O espelho avisa a troca de estado antes dos eventos do mesmo frame: o contador separa um match_update do outro.
            updates++;
            Act();
        }

        private void OnEvent(MatchEvent matchEvent)
        {
            UserId? self = match.Mirror.Self;
            if (self == null)
                return;

            coverage.NoteEvent(Label, self.Value, matchEvent);
            if (matchEvent is TurnTimedOutEvent timedOut)
                OnTurnTimedOut(timedOut, self.Value);
            else if (matchEvent is PassedEvent passed && lastTimeout?.User == passed.User && lastTimeout?.Update == updates)
                SawTimedOutPass = true;
        }

        private void OnTurnTimedOut(TurnTimedOutEvent timedOut, UserId self)
        {
            TimedOutTurns.Add(timedOut.TurnNumber);
            lastTimeout = (timedOut.User, updates);
            if (timedOut.User != self)
                return;

            Strategy.NoteTurnTimedOut();
            Act();
        }

        private void OnRefused(PlayRefusal refusal)
        {
            Refusals.Add(refusal);
            coverage.NoteRefused(Label, refusal);
            Act();
        }

        private void OnStatus(LiveMatchStatus status)
        {
            // Match_start de mesma versão não troca o estado: a volta a ao vivo também é hora de agir.
            if (status.Phase != LiveMatchPhase.Live)
                return;

            // Primeira prova contra o servidor (2026-09-14): P2 mandou confirm_attack na versão 25, a queda do passo 11
            // levou o envio, e na volta a estratégia achava tudo já tentado nessa versão.
            Strategy.ForgetTried();
            Act();
        }

        private void Act()
        {
            PlayerView? view = match.Mirror.Current;
            if (view == null || match.Mirror.IsFinished || Failure != null)
                return;

            try
            {
                Send(Strategy.Next(view, match.Mirror.Version ?? 0));
            }
            catch (InvalidOperationException stuck)
            {
                Failure = stuck;
            }
        }

        private void Send(PlayCommand? command)
        {
            if (command == null)
                return;

            Sent[command.MessageType] = Sent.TryGetValue(command.MessageType, out int count) ? count + 1 : 1;
            coverage.NoteSent(Label, command);
            _ = match.Commands.Send(command);
        }
    }
}
