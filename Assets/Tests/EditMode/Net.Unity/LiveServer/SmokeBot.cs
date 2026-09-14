#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Match;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Um bot headless do marco: a cada estado substituído, recusa ou volta a ao vivo, pede à estratégia o próximo
    /// comando e o manda pela sessão. Guarda envios, recusas e a primeira falha da estratégia para o teste verificar.
    /// </summary>
    internal sealed class SmokeBot : IDisposable
    {
        private readonly LiveMatch match;
        private readonly SmokeStrategy strategy;

        internal SmokeBot(string label, LiveMatch match, SmokeStrategy strategy)
        {
            Label = label;
            this.match = match;
            this.strategy = strategy;
            match.Mirror.ViewReplaced += OnViewReplaced;
            match.Refused += OnRefused;
            match.StatusChanged += OnStatus;
        }

        internal string Label { get; }

        internal List<PlayRefusal> Refusals { get; } = new List<PlayRefusal>();

        internal Dictionary<string, int> Sent { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        internal Exception? Failure { get; private set; }

        public void Dispose()
        {
            match.Mirror.ViewReplaced -= OnViewReplaced;
            match.Refused -= OnRefused;
            match.StatusChanged -= OnStatus;
        }

        private void OnViewReplaced(ViewReplaced change) => Act();

        private void OnRefused(PlayRefusal refusal)
        {
            Refusals.Add(refusal);
            Act();
        }

        private void OnStatus(LiveMatchStatus status)
        {
            // Match_start de mesma versão não troca o estado: a volta a ao vivo também é hora de agir.
            if (status.Phase == LiveMatchPhase.Live)
                Act();
        }

        private void Act()
        {
            PlayerView? view = match.Mirror.Current;
            if (view == null || match.Mirror.IsFinished || Failure != null)
                return;

            try
            {
                Send(strategy.Next(view, match.Mirror.Version ?? 0));
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
            _ = match.Commands.Send(command);
        }
    }
}
