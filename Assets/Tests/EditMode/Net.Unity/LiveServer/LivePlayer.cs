#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
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

        internal LivePlayer(LiveNetworkAdapters adapters, IFrameTicker ticker, string vaultDirectory, bool spoilFirstToken)
        {
            Lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());
            RefreshTokenVaultSlot slot = RefreshTokenVaultSlot.Named("live-queue-" + Guid.NewGuid().ToString("N").Substring(0, 12));
            Account = new LiveAccountServices(adapters.Http, adapters.Codec, adapters.Clock, Lifecycle, Log, LocalAccountRoutes.Create(),
                new DpapiRefreshTokenVault(vaultDirectory, slot), new AccountTiming());
            IAccessTokenSource tokens = spoilFirstToken ? new SpoiledFirstTokenSource(Account.Tokens) : Account.Tokens;
            ConnectionPorts ports = new ConnectionPorts(adapters.Sockets, adapters.Clock, ticker, Lifecycle, Reachability, adapters.Queue, Log);
            Connections = new LiveConnectionServices(ports, tokens, adapters.Codec, LocalConnectionRoutes.Create());
            Connections.Queue.Paired += Pairings.Add;
            Connections.Queue.Refused += Refusals.Add;
        }

        internal FakeClientLog Log { get; } = new FakeClientLog();

        internal FakeNetworkReachability Reachability { get; } = new FakeNetworkReachability(NetworkKind.LocalArea);

        internal FakeAppLifecycle Lifecycle { get; }

        internal LiveAccountServices Account { get; }

        internal LiveConnectionServices Connections { get; }

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

        public void Dispose()
        {
            Connections.Dispose();
            Account.Dispose();
        }
    }
}
#endif
