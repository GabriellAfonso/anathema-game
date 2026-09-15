#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>
    /// A fachada composta sobre os fakes nomeados, com o codec real de fila e partida. Um lugar só para compor e
    /// roteirizar (login, fila, pareamento, partida, queda, fim, expiração), para os testes de estágio, conta, fila,
    /// partida, histórico e saúde não repetirem a montagem.
    /// </summary>
    internal sealed class FacadeTestRig
    {
        internal const string HttpBase = "http://127.0.0.1:8000";
        internal const string WsBase = "ws://127.0.0.1:8000";
        internal const string MatchIdText = "match-7";
        internal const string MatchFoundFrame = "{\"type\": \"match_found\", \"payload\": {\"self\": {\"user_id\": 7, \"nickname\": \"one\", \"icon\": \"default\", \"level\": 1}, \"opponent\": {\"user_id\": 9, \"nickname\": \"two\", \"icon\": \"knight\", \"level\": 3}, \"match_id\": \"match-7\"}}";
        internal const string MatchStart = "contract-match-start-mulligan.json";
        internal const string MatchFinished = "contract-match-update-finished.json";

        internal FacadeTestRig(FakeRefreshTokenVault? vault = null)
        {
            Vault = vault ?? new FakeRefreshTokenVault();
            Lifecycle = new FakeAppLifecycle(Clock);
            Queue = new MainThreadQueue(Log);
            Codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), Log);
            Ports = new ClientPorts(Http, Sockets, Clock, Ticker, Lifecycle, Reachability, Queue, Log, Codec, Vault, AccountRoutesForTests(), ConnectionRoutesForTests(), new AccountTiming());
            Client = new AnathemaClient(Ports);
        }

        internal FakeHttpTransport Http { get; } = new FakeHttpTransport();

        internal FakeWebSocketFactory Sockets { get; } = new FakeWebSocketFactory();

        internal FakeMonotonicClock Clock { get; } = new FakeMonotonicClock();

        internal FakeFrameTicker Ticker { get; } = new FakeFrameTicker();

        internal FakeNetworkReachability Reachability { get; } = new FakeNetworkReachability(NetworkKind.LocalArea);

        internal FakeClientLog Log { get; } = new FakeClientLog();

        internal FakeRefreshTokenVault Vault { get; }

        internal FakeAppLifecycle Lifecycle { get; }

        internal MainThreadQueue Queue { get; }

        internal IProtocolCodec Codec { get; }

        internal ClientPorts Ports { get; }

        internal AnathemaClient Client { get; }

        internal static AccountRoutes AccountRoutesForTests()
        {
            return new AccountRoutes(Url("/accounts/register/"), Url("/accounts/login/"), Url("/accounts/token/refresh/"), Url("/players/me/"),
                Url("/game/cards/"), Url("/players/decks/"), Url("/game/matches/"));
        }

        internal static ConnectionRoutes ConnectionRoutesForTests()
        {
            return new ConnectionRoutes(new Uri(WsBase + "/ws/matchmaking/"), new Uri(WsBase + "/ws/match/"));
        }

        internal static string MatchFixture(string name)
        {
            string path = FixturePath(name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"fixture '{name}' not found at '{path}': expected a .json file in Assets/Tests/EditMode/Net.Match/Fixtures", path);

            return File.ReadAllText(path);
        }

        internal void Pump() => Queue.Drain();

        internal void Advance(TimeSpan duration)
        {
            Clock.Advance(duration);
            Ticker.Tick();
            Queue.Drain();
        }

        internal void OpenLatest()
        {
            Sockets.Latest.SimulateOpened();
            Pump();
        }

        internal void Receive(string frameJson)
        {
            Sockets.Latest.SimulateText(frameJson);
            Pump();
        }

        internal void ReceiveMatchFixture(string name) => Receive(MatchFixture(name));

        internal async Task SignInAsync()
        {
            Http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));
            SignInOutcome outcome = await Client.Account.SignInAsync("one", new Password("123456"));
            Pump();
            if (outcome.Kind != SignInOutcomeKind.SignedIn)
                throw new InvalidOperationException($"rig sign in ended as {outcome}: expected SignedIn");
        }

        internal void JoinQueue()
        {
            Client.Queue.Join(new DeckId(4));
            Pump();
            OpenLatest();
        }

        internal void ReceiveMatchFound(int catalogStatus = 200)
        {
            // A abertura da partida pede o catálogo logo depois do pareamento.
            Http.RespondNext(catalogStatus, catalogStatus == 200 ? FacadeCatalog.Body() : "{\"detail\": \"falhou\"}");
            Receive(MatchFoundFrame);
            Pump();
        }

        internal async Task ReachPairedAsync()
        {
            await SignInAsync();
            JoinQueue();
            ReceiveMatchFound();
        }

        internal async Task ReachInMatchAsync()
        {
            await ReachPairedAsync();
            OpenLatest();
            ReceiveMatchFixture(MatchStart);
        }

        internal async Task ReachStageAsync(string stage)
        {
            await SignInAsync();
            if (stage == nameof(ClientStage.SignedIn))
                return;

            JoinQueue();
            if (stage != nameof(ClientStage.Searching))
                await ReachMatchStageAsync(stage);

            AssertStage(stage);
        }

        internal async Task ExpireSessionAsync()
        {
            Http.RespondNext(401, "{\"detail\": \"Token is invalid or expired\"}");
            await Client.AccountParts.Tokens.RenewNowAsync();
            Pump();
        }

        internal void DropMatchSocketAndReopen()
        {
            Sockets.Latest.SimulateClosed(null, "queda");
            // A política de partida espera no máximo 15 s entre tentativas (ConnectionSettings.ForMatch).
            Advance(TimeSpan.FromSeconds(20));
            OpenLatest();
        }

        internal LiveMatch NewLiveMatch(string matchId)
        {
            return new LiveMatch(Client.ConnectionParts.MatchConnection, Ports.ConnectionRoutes.Match, new MatchId(matchId), FacadeCatalog.Default(Log), Clock, Log);
        }

        internal int RequestsTo(Uri url) => Http.Requests.Count(request => request.Url == url);

        internal int HistoryReads() => Http.Requests.Count(request => request.Url.AbsolutePath == "/game/matches/");

        internal static string HistoryPage(bool withRow, bool won = true, string opponent = "{\"user_id\": 9, \"nickname\": \"two\", \"icon\": \"knight\", \"level\": 3}")
        {
            string other = "{\"match_id\": \"older-match\", \"won\": false, \"end_reason\": \"forfeit\", \"opponent\": null, \"duration_seconds\": 63, \"final_round\": 1, \"ended_at\": \"2026-09-11T22:47:02Z\"}";
            string row = "{\"match_id\": \"" + MatchIdText + "\", \"won\": " + (won ? "true" : "false") + ", \"end_reason\": \"nexus_depleted\", \"opponent\": " + opponent
                + ", \"duration_seconds\": 742, \"final_round\": 8, \"ended_at\": \"2026-09-12T18:03:11Z\"}";
            string rows = withRow ? row + ", " + other : other;
            return "{\"count\": 2, \"next\": null, \"previous\": null, \"results\": [" + rows + "]}";
        }

        internal async Task ReachFinishedAsync(params string[] historyBodies)
        {
            await ReachInMatchAsync();
            // A primeira leitura do histórico sai no mesmo frame do fim: as respostas precisam estar roteirizadas antes.
            foreach (string body in historyBodies)
                Http.RespondNext(200, body);

            ReceiveMatchFixture(MatchFinished);
        }

        internal int Count(string eventName) => Log.Entries.Count(entry => entry.EventName == eventName);

        internal string LogValue(string eventName, string field)
        {
            foreach (LogField logField in Log.Entries.Last(entry => entry.EventName == eventName).Fields)
            {
                if (logField.Name == field)
                    return logField.Value;
            }

            throw new InvalidOperationException($"log event '{eventName}' has no field '{field}': expected the fields of contracts/client-state.md");
        }

        private async Task ReachMatchStageAsync(string stage)
        {
            ReceiveMatchFound(stage == nameof(ClientStage.MatchUnavailable) ? 500 : 200);
            if (stage == nameof(ClientStage.Paired) || stage == nameof(ClientStage.MatchUnavailable))
                return;

            OpenLatest();
            ReceiveMatchFixture(stage == nameof(ClientStage.MatchFinished) ? MatchFinished : MatchStart);
            await Task.CompletedTask;
        }

        private void AssertStage(string stage)
        {
            if (Client.State.Stage.ToString() != stage)
                throw new InvalidOperationException($"rig reached {Client.State.Stage}: expected {stage}");
        }

        private static Uri Url(string path) => new Uri(HttpBase + path);

        private static string FixturePath(string name, [CallerFilePath] string sourcePath = "")
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "..", "Net.Match", "Fixtures", name);
        }
    }
}
