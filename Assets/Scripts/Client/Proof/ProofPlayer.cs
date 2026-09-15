#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using Anathema.Net.Match;
using Anathema.Net.Unity;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// Um jogador da prova: a fachada composta pelo mesmo caminho do jogo, com slot próprio da guarda e <see cref="ProofLog"/>,
    /// os avisos que o roteiro confere e o bot da partida corrente (specs/005-presentation-facade/contracts/match-proof.md, "Montagem").
    /// </summary>
    internal sealed class ProofPlayer : IDisposable
    {
        private readonly ProofSetup setup;
        private readonly bool allowClockJumps;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        internal ProofPlayer(string label, ProofSetup setup, bool allowClockJumps)
        {
            Label = label;
            this.setup = setup;
            this.allowClockJumps = allowClockJumps;
            Slot = RefreshTokenVaultSlot.Named($"proof-{label.ToLowerInvariant()}-{setup.Suffix}");
            Log = new ProofLog(setup.ConsoleLog);
            Composed = Compose();
        }

        internal string Label { get; }

        internal RefreshTokenVaultSlot Slot { get; }

        internal ProofLog Log { get; }

        internal ComposedClient Composed { get; private set; }

        internal AnathemaClient Client => Composed.Client;

        internal UserId? User { get; set; }

        internal LoadedCatalog? Catalog { get; set; }

        internal DeckId SpellDeck { get; set; }

        internal ProofBot? Bot { get; private set; }

        internal List<QueueRefusal> QueueRefusals { get; } = new List<QueueRefusal>();

        internal List<MatchPairing> Pairings { get; } = new List<MatchPairing>();

        internal List<MatchResult> Results { get; } = new List<MatchResult>();

        internal int StageChangeCount { get; private set; }

        internal int ReconnectingCount { get; private set; }

        internal int RecoveredCount { get; private set; }

        internal void Pump() => Composed.Pump();

        internal void Recompose()
        {
            StopBot();
            Unsubscribe();
            Composed.Dispose();
            Composed = Compose();
        }

        internal ProofBot StartBot(CommandCoverage coverage, Action<ProofStrategy> adjust)
        {
            LiveMatch match = Client.CurrentMatch ?? throw new InvalidOperationException($"{Label} has no current match in stage {Client.State.Stage}: expected InMatch");
            LoadedCatalog catalog = Catalog ?? throw new InvalidOperationException($"{Label} has no catalog: expected step 3 to load it");
            ProofStrategy strategy = new ProofStrategy(Label, catalog, match.HintFor, coverage);
            adjust(strategy);
            StopBot();
            Bot = new ProofBot(Label, match, strategy, coverage);
            return Bot;
        }

        internal void StopBot()
        {
            Bot?.Dispose();
            Bot = null;
        }

        internal string Describe()
        {
            PlayerView? view = Client.CurrentMatch?.Mirror.Current;
            return $"{Label} stage={Client.State.Stage} round={view?.RoundNumber.ToString() ?? "-"} phase={view?.Phase.ToString() ?? "-"}";
        }

        public void Dispose()
        {
            StopBot();
            // Sair apaga a guarda do slot da execução; num roteiro que já saiu no passo 18, a chamada não se aplica.
            Client.Account.SignOut();
            Unsubscribe();
            Composed.Dispose();
        }

        private ComposedClient Compose()
        {
            ClientCompositionOptions options = new ClientCompositionOptions(setup.Routes, setup.AllowCleartext) { VaultSlot = Slot, Log = Log, AllowClockJumps = allowClockJumps };
            ComposedClient composed = ClientComposition.Compose(options);
            Subscribe(composed.Client);
            setup.Attach?.Invoke(Label, composed);
            return composed;
        }

        private void Subscribe(AnathemaClient client)
        {
            subscriptions.Add(client.Queue.Refused.Subscribe(QueueRefusals.Add));
            subscriptions.Add(client.Queue.Paired.Subscribe(Pairings.Add));
            subscriptions.Add(client.ResultUpdated.Subscribe(Results.Add));
            subscriptions.Add(client.StageChanged.Subscribe(_ => StageChangeCount++));
            subscriptions.Add(client.Health.Reconnecting.Subscribe(_ => ReconnectingCount++));
            subscriptions.Add(client.Health.Recovered.Subscribe(_ => RecoveredCount++));
        }

        private void Unsubscribe()
        {
            foreach (IDisposable subscription in subscriptions)
                subscription.Dispose();

            subscriptions.Clear();
        }
    }
}
