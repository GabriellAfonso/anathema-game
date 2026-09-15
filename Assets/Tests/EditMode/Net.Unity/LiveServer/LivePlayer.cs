#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using Anathema.Net.Fakes;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Uma conta nova do backend local com as duas conexões reais. Ciclo de vida e rede são fakes, porque
    /// sinais do sistema não são produzíveis num teste de editor (specs/003-authenticated-socket-queue/research.md, R14).
    /// </summary>
    internal sealed class LivePlayer : IDisposable
    {
        private const string Password = "live-queue-123";
        private const string SpellDeckName = "Fumaça";

        internal LivePlayer(LiveNetworkAdapters adapters, IFrameTicker ticker, string vaultDirectory, bool spoilFirstToken, IWebSocketFactory? sockets = null)
        {
            Adapters = adapters;
            Lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());
            RefreshTokenVaultSlot slot = RefreshTokenVaultSlot.Named("live-queue-" + Guid.NewGuid().ToString("N").Substring(0, 12));
            ClientPorts ports = new ClientPorts(adapters.Http, sockets ?? adapters.Sockets, adapters.Clock, ticker, Lifecycle, Reachability, adapters.Queue, Log,
                adapters.Codec, new DpapiRefreshTokenVault(vaultDirectory, slot), LocalAccountRoutes.Create(), LocalConnectionRoutes.Create(), new AccountTiming());
            Account = new AccountServices(ports);
            IAccessTokenSource tokens = spoilFirstToken ? new SpoiledFirstTokenSource(Account.Tokens) : Account.Tokens;
            Connections = new ConnectionServices(ports, tokens);
            Connections.Queue.Paired += Pairings.Add;
            Connections.Queue.Refused += Refusals.Add;
        }

        internal LiveNetworkAdapters Adapters { get; }

        internal FakeClientLog Log { get; } = new FakeClientLog();

        internal FakeNetworkReachability Reachability { get; } = new FakeNetworkReachability(NetworkKind.LocalArea);

        internal FakeAppLifecycle Lifecycle { get; }

        internal AccountServices Account { get; }

        internal ConnectionServices Connections { get; }

        internal List<MatchPairing> Pairings { get; } = new List<MatchPairing>();

        internal List<QueueRefusal> Refusals { get; } = new List<QueueRefusal>();

        internal int Count(string eventName) => Log.Entries.Count(entry => entry.EventName == eventName);

        /// <summary>Cadastra, entra e devolve o deck inicial que o servidor cria para a conta nova.</summary>
        internal async Task<DeckId> SignUpAsync()
        {
            string username = "queue_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            RegistrationForm form = new RegistrationForm(username, username + "@live.local", new Password(Password), new Password(Password));
            Assert.That((await Account.Registration.RegisterAsync(form)).IsSuccess, Is.True, "register " + username);
            SignInOutcome signedIn = await Account.Session.SignInAsync(username, new Password(Password));
            Assert.That(signedIn.Kind, Is.EqualTo(SignInOutcomeKind.SignedIn), signedIn.ToString());
            AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks = await Account.Decks.ListAsync();
            Assert.That(decks.IsSuccess, Is.True, "list decks");
            return decks.Value.First().Deck;
        }

        /// <summary>
        /// Cadastra e cria o deck com feitiços de <c>spell_deck_id</c> em <c>backend/scripts/smoke_match.py</c>: o deck
        /// inicial não tem feitiço nenhum, e sem isto a partida nunca lança um.
        /// </summary>
        internal async Task<DeckId> SignUpWithSpellDeckAsync()
        {
            await SignUpAsync();
            AccountCallOutcome<PlayerDeck, DeckRefusal> created = await Account.Decks.CreateAsync(new DeckDraft(SpellDeckName, SpellDeckCards()));
            Assert.That(created.IsSuccess, Is.True, "create spell deck: " + created.Refusal);
            return created.Value.Deck;
        }

        /// <summary>O catálogo da sessão desta conta.</summary>
        internal async Task<LoadedCatalog> LoadCatalogAsync()
        {
            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded = await Account.Catalog.LoadAsync();
            Assert.That(loaded.IsSuccess, Is.True, "load catalog: " + (loaded.Failure?.ToString() ?? loaded.Refusal?.ToString()));
            return loaded.Value;
        }

        /// <summary>A sessão de partida sobre a conexão de partida desta conta, sem começar.</summary>
        internal LiveMatch OpenMatch(MatchId match, LoadedCatalog catalog)
        {
            return new LiveMatch(Connections.MatchConnection, Connections.Routes.Match, match, catalog, Adapters.Clock, Log);
        }

        public void Dispose()
        {
            Connections.Dispose();
            Account.Dispose();
        }

        private static IReadOnlyList<CardId> SpellDeckCards()
        {
            IEnumerable<long> units = Enumerable.Range(1, 8).SelectMany(card => Enumerable.Repeat((long)card, 3)).Append(9);
            IEnumerable<long> spells = Enumerable.Range(1001, 5).SelectMany(card => Enumerable.Repeat((long)card, 3));
            return units.Concat(spells).Select(card => new CardId(card)).ToArray();
        }
    }
}
#endif
