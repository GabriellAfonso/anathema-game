#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A superfície única que a apresentação consome: conta, catálogo, decks, histórico, fila, partida corrente,
    /// saúde da conexão e o estado do app. Composta por construtor a partir das peças das features 001–004, sem
    /// singleton; a lista do que se pode assinar e chamar está em
    /// <c>specs/005-presentation-facade/contracts/presentation-surface.md</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// public void BindClient(AnathemaClient client)
    /// {
    ///     subscriptions.Add(client.StageChanged.Subscribe(change => Show(change.Current)));
    ///     Show(client.State);
    /// }
    /// </code>
    /// </example>
    public sealed class AnathemaClient : IDisposable
    {
        private readonly ClientStages stages;
        private readonly MatchOpening opening;
        private readonly HistoryRowLookup lookup;
        private readonly HealthRelay relay;
        private bool disposed;

        internal AnathemaClient(ClientPorts ports)
        {
            Ports = ports ?? throw new ArgumentNullException(nameof(ports), "ports are null: expected the ClientPorts built by the composition");
            AccountParts = new AccountServices(ports);
            ConnectionParts = new ConnectionServices(ports, AccountParts.Tokens);
            stages = new ClientStages(ports.Log);
            relay = new HealthRelay(ConnectionParts, stages, ports.Log);
            ResultUpdated = new EventFeed<MatchResult>("match_result_updated", ports.Log);
            Expiry = new SessionExpiryWatch(AccountParts.Session, ConnectionParts.Queue, stages, ports.Queue, TearDown);
            opening = new MatchOpening(AccountParts, ConnectionParts, ports, stages, Expiry);
            lookup = new HistoryRowLookup(AccountParts.History, ports, stages, ResultUpdated.Publish);
            // Primeiro ouvinte do estágio: a busca da linha começa antes de qualquer tela reagir ao fim.
            stages.StageChanged.Subscribe(StartLookupOnFinish);
            Account = new ClientAccount(AccountParts, stages, ports.Queue, TearDown);
            Queue = new ClientQueue(ConnectionParts.Queue, stages, ports.Log, opening.Open);
        }

        /// <summary>O estado do app agora.</summary>
        /// <example><code>ClientStage stage = client.State.Stage;</code></example>
        public ClientState State => stages.State;

        /// <summary>Cada transição do estado do app, na thread principal.</summary>
        /// <example><code>subscriptions.Add(client.StageChanged.Subscribe(change => Show(change.Current)));</code></example>
        public EventFeed<ClientStageChange> StageChanged => stages.StageChanged;

        /// <summary>A linha do histórico da partida terminada chegou ou ficou indisponível; o estágio continua o mesmo.</summary>
        /// <example><code>subscriptions.Add(client.ResultUpdated.Subscribe(result => ShowRounds(result.Row?.FinalRound)));</code></example>
        public EventFeed<MatchResult> ResultUpdated { get; }

        /// <summary>A conta: entrar, retomar, sair, cadastrar e perfil.</summary>
        /// <example><code>SignInOutcome outcome = await client.Account.SignInAsync(username, password);</code></example>
        public ClientAccount Account { get; }

        /// <summary>O catálogo de cartas, carregado uma vez por sessão; consulta por <c>CardId</c> no <see cref="LoadedCatalog"/>.</summary>
        /// <example><code>AccountCallOutcome&lt;LoadedCatalog, UnrecognizedRefusal&gt; cards = await client.Catalog.LoadAsync();</code></example>
        public CardCatalog Catalog => AccountParts.Catalog;

        /// <summary>Os decks do jogador.</summary>
        /// <example><code>AccountCallOutcome&lt;IReadOnlyList&lt;PlayerDeck&gt;, DeckRefusal&gt; decks = await client.Decks.ListAsync();</code></example>
        public PlayerDecks Decks => AccountParts.Decks;

        /// <summary>O histórico de partidas.</summary>
        /// <example><code>AccountCallOutcome&lt;MatchHistoryPage, HistoryRefusal&gt; page = await client.History.ReadPageAsync(new HistoryPageRequest());</code></example>
        public MatchHistory History => AccountParts.History;

        /// <summary>A fila: entrar, sair e os avisos de pareado, recusa e saída.</summary>
        /// <example><code>QueueJoinResult result = client.Queue.Join(deck);</code></example>
        public ClientQueue Queue { get; }

        /// <summary>
        /// A partida corrente, ou nenhuma. Fica fora da cena de partida porque o <c>match_start</c> chega antes de
        /// ela existir, e chega de novo a cada reconexão, com a cena já montada: a cena lê o espelho quando liga e se
        /// redesenha a cada estado substituído. O cliente nunca mescla: cada estado substitui o anterior inteiro,
        /// porque o servidor é a autoridade (comentários do antigo <c>MatchSession</c>).
        /// </summary>
        /// <example><code>PlayerView? view = client.CurrentMatch?.Mirror.Current;</code></example>
        public LiveMatch? CurrentMatch => stages.State.Match;

        /// <summary>A saúde da conexão ativa, para o overlay de reconexão.</summary>
        /// <example><code>subscriptions.Add(client.Health.Reconnecting.Subscribe(notice => ShowRetrying(notice.Attempt)));</code></example>
        public ConnectionHealth Health => relay.Health;

        /// <summary>O log da camada, para a apresentação registrar com campos.</summary>
        /// <example><code>client.Log.Warning("play_without_deck");</code></example>
        public IClientLog Log => Ports.Log;

        internal ClientPorts Ports { get; }

        internal AccountServices AccountParts { get; }

        internal ConnectionServices ConnectionParts { get; }

        internal SessionExpiryWatch Expiry { get; }

        internal ClientStages Stages => stages;

        /// <summary>Abre de novo a partida indisponível, com o mesmo pareamento; fora disso não faz nada.</summary>
        /// <example><code>StageRequestResult result = client.RetryMatch();</code></example>
        public StageRequestResult RetryMatch()
        {
            ClientState state = stages.State;
            if (state.Stage != ClientStage.MatchUnavailable)
                return StageRequestResult.NotApplicable(state.Stage);

            stages.Invalidate();
            opening.Close();
            stages.Move(ClientState.Paired(state.Self!.Value, state.Pairing!), "match_retry");
            opening.Open(state.Pairing!);
            return StageRequestResult.Done(ClientStage.Paired);
        }

        /// <summary>Volta ao início depois do fim ou da indisponibilidade da partida; fora disso não faz nada.</summary>
        /// <example><code>StageRequestResult result = client.ReturnToLobby();</code></example>
        public StageRequestResult ReturnToLobby()
        {
            ClientState state = stages.State;
            if (state.Stage != ClientStage.MatchFinished && state.Stage != ClientStage.MatchUnavailable)
                return StageRequestResult.NotApplicable(state.Stage);

            CloseMatch();
            stages.Move(ClientState.SignedIn(state.Self!.Value), "returned_to_lobby");
            return StageRequestResult.Done(ClientStage.SignedIn);
        }

        /// <summary>Fecha fila e partida de propósito e para de ouvir; só o hospedeiro chama.</summary>
        /// <example><code>client.Dispose();</code></example>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            CloseMatch();
            lookup.Dispose();
            relay.Dispose();
            Expiry.Dispose();
            ConnectionParts.Dispose();
            AccountParts.Dispose();
        }

        private void TearDown(SignedOutReason reason, string cause)
        {
            // Sair e expirar fecham tudo de propósito; a partida continua no servidor e termina pelos relógios dele.
            CloseMatch();
            ConnectionParts.Queue.Leave();
            stages.Move(ClientState.SignedOut(reason), cause);
        }

        private void CloseMatch()
        {
            stages.Invalidate();
            lookup.Stop();
            opening.Close();
        }

        private void StartLookupOnFinish(ClientStageChange change)
        {
            if (change.Current.Stage == ClientStage.MatchFinished && change.Previous.Stage != ClientStage.MatchFinished)
                lookup.Start(change.Current.Result!);
        }
    }
}
