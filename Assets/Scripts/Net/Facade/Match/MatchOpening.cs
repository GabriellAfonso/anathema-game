#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Abre a partida pareada e segue a sessão dela até o fim. Carrega o catálogo antes, cria e começa a
    /// <see cref="LiveMatch"/>, e leva o app a <see cref="ClientStage.InMatch"/> no primeiro estado, a
    /// <see cref="ClientStage.MatchFinished"/> no frame final e a <see cref="ClientStage.MatchUnavailable"/> quando
    /// o catálogo falha ou a conexão desiste. Veio do antigo <c>MatchClient</c>
    /// (specs/005-presentation-facade/contracts/composition-and-scenes.md, "Destino do código antigo").
    /// </summary>
    internal sealed class MatchOpening
    {
        private readonly AccountServices account;
        private readonly ConnectionServices connections;
        private readonly ClientPorts ports;
        private readonly ClientStages stages;
        private readonly SessionExpiryWatch expiry;
        private LiveMatch? live;
        private IDisposable? statusSubscription;

        internal MatchOpening(AccountServices account, ConnectionServices connections, ClientPorts ports, ClientStages stages, SessionExpiryWatch expiry)
        {
            this.account = account;
            this.connections = connections;
            this.ports = ports;
            this.stages = stages;
            this.expiry = expiry;
        }

        internal void Open(MatchPairing pairing)
        {
            _ = LoadCatalogAsync(pairing, stages.Generation);
        }

        internal void Close()
        {
            statusSubscription?.Dispose();
            statusSubscription = null;
            live?.Dispose();
            live = null;
        }

        private async Task LoadCatalogAsync(MatchPairing pairing, int generation)
        {
            try
            {
                // O catálogo fica em cache por geração de sessão: com a Home já tendo carregado, isto volta na hora.
                AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded = await account.Catalog.LoadAsync();
                ports.Queue.Enqueue(() => AfterCatalog(pairing, generation, loaded));
            }
            catch (Exception failure)
            {
                ports.Log.Error("match_open_failed", new LogField("match_id", pairing.Match.Value), new LogField("exception", failure.GetType().Name), new LogField("message", failure.Message));
            }
        }

        private void AfterCatalog(MatchPairing pairing, int generation, AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded)
        {
            if (generation != stages.Generation || stages.State.Stage != ClientStage.Paired)
                return;

            if (loaded.IsSuccess)
                StartLive(pairing, loaded.Value);
            else
                OnCatalogFailed(pairing, loaded);
        }

        private void OnCatalogFailed(MatchPairing pairing, AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded)
        {
            if (loaded.Failure?.Session != null)
            {
                expiry.Expire("catalog_session_unavailable");
                return;
            }

            // Antes só ia para o log e o servidor estourava o mulligan sozinho; agora é estado explícito (FR-014).
            ports.Log.Error("match_catalog_unavailable", new LogField("match_id", pairing.Match.Value), new LogField("kind", loaded.Failure?.Kind.ToString() ?? "refused"));
            MatchUnavailable unavailable = MatchUnavailable.CatalogUnavailable(pairing.Match, loaded.Failure);
            stages.Move(ClientState.MatchUnavailable(stages.State.Self!.Value, pairing, unavailable), "catalog_unavailable");
        }

        private void StartLive(MatchPairing pairing, LoadedCatalog catalog)
        {
            Close();
            LiveMatch match = new LiveMatch(connections.MatchConnection, connections.Routes.Match, pairing.Match, catalog, ports.Clock, ports.Log);
            live = match;
            statusSubscription = match.StatusChanged.Subscribe(status => OnStatus(match, status));
            // Antes de começar: quem desenha lê a partida corrente quando o estágio muda, então ela precisa já estar lá.
            stages.Replace(stages.State.WithOpenedMatch(match));
            match.Start();
        }

        private void OnStatus(LiveMatch match, LiveMatchStatus status)
        {
            if (!ReferenceEquals(match, live))
                return;

            if (status.Phase == LiveMatchPhase.Live)
                EnterInMatch(match);
            else if (status.Phase == LiveMatchPhase.Finished)
                EnterFinished(match, status);
            else if (status.GiveUp != null)
                EnterUnavailable(status);
        }

        private void EnterInMatch(LiveMatch match)
        {
            // Queda e volta com match_start não mudam o estágio: só a primeira vez ao vivo leva à partida.
            ClientState state = stages.State;
            if (state.Stage == ClientStage.Paired)
                stages.Move(ClientState.InMatch(state.Self!.Value, state.Pairing!, match), "match_live");
        }

        private void EnterFinished(LiveMatch match, LiveMatchStatus status)
        {
            ClientState state = stages.State;
            if (!IsOpenOrPlaying(state))
                return;

            MatchResult result = MatchResult.Fetching(match.Match, status.Outcome, match.Mirror.DidIWin ?? false);
            stages.Move(ClientState.MatchFinished(state.Self!.Value, state.Pairing!, match, result), "match_finished");
        }

        private void EnterUnavailable(LiveMatchStatus status)
        {
            GiveUpReason reason = status.GiveUp!;
            ClientState state = stages.State;
            if (expiry.NoticeGiveUp(reason) || !IsOpenOrPlaying(state))
                return;

            MatchId match = state.Pairing!.Match;
            MatchUnavailable unavailable = status.Phase == LiveMatchPhase.Refused ? MatchUnavailable.MatchRefused(match, reason) : MatchUnavailable.ConnectionGaveUp(match, reason);
            Close();
            stages.Move(ClientState.MatchUnavailable(state.Self!.Value, state.Pairing, unavailable), "match_unavailable");
        }

        private static bool IsOpenOrPlaying(ClientState state) => state.Stage == ClientStage.Paired || state.Stage == ClientStage.InMatch;
    }
}
