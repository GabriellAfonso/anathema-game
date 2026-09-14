#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Match;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// O marco jogável da feature 004: dois clientes headless jogam uma partida inteira contra o backend local,
    /// com uma queda provocada no meio (specs/004-match-session/research.md, R14; quickstart §2).
    /// </summary>
    [Explicit, Category("LiveServer")]
    public class LiveMatchTests
    {
        private const int RefusalLimit = 20;
        private const long DropRound = 3;
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(600);

        private FakeClientLog log = null!;
        private MainThreadQueue queue = null!;
        private LiveNetworkAdapters adapters = null!;
        private FakeFrameTicker ticker = null!;
        private List<IDisposable> owned = null!;
        private string vaultDirectory = string.Empty;

        [SetUp]
        public void CreateLayer()
        {
            log = new FakeClientLog();
            queue = new MainThreadQueue(log);
            adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(true));
            ticker = new FakeFrameTicker();
            owned = new List<IDisposable>();
            vaultDirectory = Path.Combine(Path.GetTempPath(), "anathema-live-match-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        }

        [TearDown]
        public void CloseLayer()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
                owned[index].Dispose();

            queue.Close();
            if (Directory.Exists(vaultDirectory))
                Directory.Delete(vaultDirectory, true);
        }

        [UnityTest]
        public IEnumerator DoisBotsJogamUmaPartidaInteiraComQuedaNoMeio()
        {
            return Run(async () =>
            {
                (LivePlayer first, LivePlayer second) = await PairWithSpellDecksAsync();
                (LiveMatch firstMatch, SmokeBot firstBot, int[] firstEvents) = await OpenMatchAsync(first, "P1");
                (LiveMatch secondMatch, SmokeBot secondBot, _) = await OpenMatchAsync(second, "P2");
                List<LiveMatchPhase> secondPhases = RecordPhases(secondMatch);

                firstMatch.Start();
                secondMatch.Start();
                await PlayUntilFinishedAsync(second, firstMatch, secondMatch, firstBot, secondBot);

                AssertSameOutcome(firstMatch, secondMatch);
                AssertBotsBehaved(firstBot, secondBot);
                AssertDroppedAndCameBack(secondPhases);
                Assert.That(first.Count("match_event"), Is.EqualTo(firstEvents[0]), "uma linha de narrador por evento aceito");
                Assert.That(first.Count("connection_gave_up") + second.Count("connection_gave_up"), Is.EqualTo(0));
            });
        }

        private async Task<(LivePlayer, LivePlayer)> PairWithSpellDecksAsync()
        {
            string recording = Path.Combine("Logs", "match-recording", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            LivePlayer first = NewPlayer(new RecordingWebSocketFactory(adapters.Sockets, recording));
            LivePlayer second = NewPlayer(adapters.Sockets);
            first.Connections.Queue.Join(await first.SignUpWithSpellDeckAsync());
            second.Connections.Queue.Join(await second.SignUpWithSpellDeckAsync());
            await UntilAsync(() => first.Pairings.Count == 1 && second.Pairings.Count == 1, TimeSpan.FromSeconds(45));
            Assert.That(first.Pairings[0].Match, Is.EqualTo(second.Pairings[0].Match), "os dois caíram na mesma partida");
            return (first, second);
        }

        private async Task<(LiveMatch, SmokeBot, int[])> OpenMatchAsync(LivePlayer player, string label)
        {
            LoadedCatalog catalog = await player.LoadCatalogAsync();
            LiveMatch match = player.OpenMatch(player.Pairings[0].Match, catalog);
            SmokeBot bot = new SmokeBot(label, match, new SmokeStrategy(label, catalog, adapters.Codec));
            int[] events = { 0 };
            match.Mirror.EventReceived += _ => events[0]++;
            owned.Add(match);
            owned.Add(bot);
            return (match, bot, events);
        }

        private async Task PlayUntilFinishedAsync(LivePlayer second, LiveMatch firstMatch, LiveMatch secondMatch, SmokeBot firstBot, SmokeBot secondBot)
        {
            bool dropped = false;
            await UntilAsync(() =>
            {
                if (!dropped && (secondMatch.Mirror.Current?.RoundNumber ?? 0) >= DropRound)
                {
                    dropped = true;
                    second.Reachability.SimulateKind(NetworkKind.CarrierData);
                }

                return (IsOver(firstMatch) && IsOver(secondMatch)) || firstBot.Failure != null || secondBot.Failure != null;
            }, MatchTimeout);
            Assert.That(firstBot.Failure ?? secondBot.Failure, Is.Null);
        }

        private static bool IsOver(LiveMatch match) => match.Status.Phase == LiveMatchPhase.Finished;

        private static void AssertSameOutcome(LiveMatch firstMatch, LiveMatch secondMatch)
        {
            MatchOutcome first = firstMatch.Status.Outcome!;
            MatchOutcome second = secondMatch.Status.Outcome!;
            Assert.That((first.DefeatedUser, first.Reason), Is.EqualTo((second.DefeatedUser, second.Reason)), "desfechos iguais");
            Assert.That(firstMatch.Mirror.DidIWin, Is.Not.EqualTo(secondMatch.Mirror.DidIWin));
        }

        private static void AssertBotsBehaved(params SmokeBot[] bots)
        {
            foreach (SmokeBot bot in bots)
            {
                Assert.That(bot.Refusals.Count, Is.LessThanOrEqualTo(RefusalLimit), $"{bot.Label} recusado demais");
                Assert.That(bot.Refusals.Select(refusal => refusal.Code), Has.None.EqualTo(PlayRefusalCode.UnknownMessageType), $"{bot.Label} mandou tipo desconhecido");
                TestContext.WriteLine($"{bot.Label} mandou {string.Join(", ", bot.Sent.Select(pair => pair.Key + "=" + pair.Value))}; recusas {bot.Refusals.Count}");
            }
        }

        private static void AssertDroppedAndCameBack(List<LiveMatchPhase> phases)
        {
            int reconnecting = phases.IndexOf(LiveMatchPhase.Reconnecting);
            Assert.That(reconnecting, Is.GreaterThanOrEqualTo(0), "a queda provocada levou a reconectando");
            Assert.That(phases.Skip(reconnecting + 1), Does.Contain(LiveMatchPhase.Live), "voltou a ao vivo depois da queda");
        }

        private static List<LiveMatchPhase> RecordPhases(LiveMatch match)
        {
            List<LiveMatchPhase> phases = new List<LiveMatchPhase>();
            match.StatusChanged += status => phases.Add(status.Phase);
            return phases;
        }

        private LivePlayer NewPlayer(IWebSocketFactory sockets)
        {
            LivePlayer player = new LivePlayer(adapters, ticker, vaultDirectory, spoilFirstToken: false, sockets);
            owned.Add(player);
            return player;
        }

        private IEnumerator Run(Func<Task> scenario)
        {
            Task running = scenario();
            Stopwatch waited = Stopwatch.StartNew();
            while (!running.IsCompleted && waited.Elapsed < MatchTimeout + TimeSpan.FromSeconds(60))
            {
                queue.Drain();
                ticker.Tick();
                yield return null;
            }

            Assert.That(running.IsCompleted, Is.True, $"scenario did not finish: is the backend up on {LocalConnectionRoutes.WsBase}?");
            if (running.IsFaulted)
                ExceptionDispatchInfo.Capture(running.Exception!.InnerException!).Throw();
        }

        private static async Task UntilAsync(Func<bool> condition, TimeSpan limit)
        {
            Stopwatch waited = Stopwatch.StartNew();
            while (!condition())
            {
                if (waited.Elapsed > limit)
                    throw new TimeoutException($"condition not met in {limit.TotalSeconds}s: expected the backend to answer on {LocalConnectionRoutes.WsBase}");

                await Task.Yield();
            }
        }
    }
}
#endif
