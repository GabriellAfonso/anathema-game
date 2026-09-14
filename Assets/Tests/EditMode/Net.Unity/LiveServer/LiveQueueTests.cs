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
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Fila e sockets contra o backend local (<c>docker compose up</c> em
    /// C:/Users/gabri/Projetos/dev_container/anathema/backend), com socket, HTTP, relógio, codec e conta
    /// reais (specs/003-authenticated-socket-queue/research.md, R14; SC-007). Fora da rodada normal.
    /// </summary>
    [Explicit, Category("LiveServer")]
    public class LiveQueueTests
    {
        private static readonly TimeSpan ScenarioTimeout = TimeSpan.FromSeconds(45);
        private FakeClientLog log = null!;
        private MainThreadQueue queue = null!;
        private LiveNetworkAdapters adapters = null!;
        private FakeFrameTicker ticker = null!;
        private List<LivePlayer> players = null!;
        private string vaultDirectory = string.Empty;

        [SetUp]
        public void CreateLayer()
        {
            log = new FakeClientLog();
            queue = new MainThreadQueue(log);
            adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(true));
            ticker = new FakeFrameTicker();
            players = new List<LivePlayer>();
            vaultDirectory = Path.Combine(Path.GetTempPath(), "anathema-live-queue-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        }

        [TearDown]
        public void CloseLayer()
        {
            players.ForEach(player => player.Dispose());
            queue.Close();
            if (Directory.Exists(vaultDirectory))
                Directory.Delete(vaultDirectory, true);
        }

        [UnityTest]
        public IEnumerator DuasContasNovasSaoPareadasNaMesmaPartida()
        {
            return Run(async () =>
            {
                (LivePlayer first, LivePlayer second) = await PairAsync();
                Assert.That(first.Pairings.Single().Match, Is.EqualTo(second.Pairings.Single().Match));
                Assert.That(first.Pairings.Single().Opponent.User, Is.EqualTo(second.Pairings.Single().Self.User));
            });
        }

        [UnityTest]
        public IEnumerator DeckInexistenteETextoSaoRecusadosESocketContinuaAberto()
        {
            return Run(async () =>
            {
                LivePlayer player = NewPlayer();
                DeckId starter = await player.SignUpAsync();
                List<MessageRefusedFrame> refused = RecordRefusals(player.Connections.MatchmakingConnection);
                player.Connections.Queue.Join(new DeckId(987654321));
                await UntilAsync(() => player.Refusals.Count == 1);
                Assert.That((player.Refusals[0].Kind, player.Refusals[0].Deck), Is.EqualTo((QueueRefusalKind.DeckNotFound, (DeckId?)new DeckId(987654321))));
                await player.Connections.MatchmakingConnection.SendAsync(new TextDeckJoinMessage("4"));
                await UntilAsync(() => refused.Count == 2);
                Assert.That(refused[1].Code, Is.EqualTo("deck_not_specified"));
                player.Connections.Queue.Join(starter);
                await UntilAsync(() => player.Connections.Queue.Phase == QueuePhase.Searching);
                await PauseAsync(TimeSpan.FromSeconds(1));
                Assert.That(refused.Count, Is.EqualTo(2), "o join_queue válido depois das recusas não foi recusado");
                player.Connections.Queue.Leave();
            });
        }

        [UnityTest]
        public IEnumerator TrocaDeRedeProcurandoVoltaParaAFilaEAindaEhPareada()
        {
            return Run(async () =>
            {
                LivePlayer first = NewPlayer();
                LivePlayer second = NewPlayer();
                DeckId firstDeck = await first.SignUpAsync();
                DeckId secondDeck = await second.SignUpAsync();
                first.Connections.Queue.Join(firstDeck);
                await UntilAsync(() => first.Connections.Queue.Phase == QueuePhase.Searching);
                first.Reachability.SimulateKind(NetworkKind.CarrierData);
                await UntilAsync(() => first.Count("queue_join_sent") == 2);
                second.Connections.Queue.Join(secondDeck);
                await UntilAsync(() => first.Pairings.Count == 1 && second.Pairings.Count == 1);
                Assert.That(first.Pairings[0].Match, Is.EqualTo(second.Pairings[0].Match));
            });
        }

        [UnityTest]
        public IEnumerator SocketDePartidaComOMatchIdRecebidoRecebeMatchStart()
        {
            return Run(async () =>
            {
                (LivePlayer first, LivePlayer _) = await PairAsync();
                AuthenticatedConnection match = first.Connections.MatchConnection;
                List<string> types = new List<string>();
                match.FrameReceived += frame => types.Add(frame.MessageType);
                match.Connect(ConnectionTarget.Match(first.Connections.Routes.Match, first.Pairings.Single().Match));
                await UntilAsync(() => types.Contains("match_start"));
                match.Leave();
            });
        }

        [UnityTest]
        public IEnumerator SocketDePartidaComMatchIdInventadoDesisteCom4404()
        {
            return Run(async () =>
            {
                LivePlayer player = NewPlayer();
                await player.SignUpAsync();
                AuthenticatedConnection match = player.Connections.MatchConnection;
                match.Connect(ConnectionTarget.Match(player.Connections.Routes.Match, new MatchId(Guid.NewGuid().ToString())));
                await UntilAsync(() => match.Status.Phase == ConnectionPhase.GaveUp);
                Assert.That(match.Status.GiveUp, Is.EqualTo(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound)));
            });
        }

        [UnityTest]
        public IEnumerator TokenDeAcessoInvalidoRenovaEConecta()
        {
            return Run(async () =>
            {
                LivePlayer player = NewPlayer(spoilFirstToken: true);
                await player.SignUpAsync();
                AuthenticatedConnection connection = player.Connections.MatchmakingConnection;
                connection.Connect(ConnectionTarget.Matchmaking(player.Connections.Routes.Matchmaking));
                await UntilAsync(() => player.Count("connection_proven") == 1);
                Assert.That(player.Count("connection_renewing_token"), Is.EqualTo(1));
                connection.Leave();
            });
        }

        private LivePlayer NewPlayer(bool spoilFirstToken = false)
        {
            LivePlayer player = new LivePlayer(adapters, ticker, vaultDirectory, spoilFirstToken);
            players.Add(player);
            return player;
        }

        private async Task<(LivePlayer, LivePlayer)> PairAsync()
        {
            LivePlayer first = NewPlayer();
            LivePlayer second = NewPlayer();
            first.Connections.Queue.Join(await first.SignUpAsync());
            second.Connections.Queue.Join(await second.SignUpAsync());
            await UntilAsync(() => first.Pairings.Count == 1 && second.Pairings.Count == 1);
            return (first, second);
        }

        private static List<MessageRefusedFrame> RecordRefusals(AuthenticatedConnection connection)
        {
            List<MessageRefusedFrame> refused = new List<MessageRefusedFrame>();
            connection.FrameReceived += frame =>
            {
                if (frame is MessageRefusedFrame refusal)
                    refused.Add(refusal);
            };
            return refused;
        }

        private IEnumerator Run(Func<Task> scenario)
        {
            Task running = scenario();
            Stopwatch waited = Stopwatch.StartNew();
            while (!running.IsCompleted && waited.Elapsed < ScenarioTimeout)
            {
                queue.Drain();
                ticker.Tick();
                yield return null;
            }

            Assert.That(running.IsCompleted, Is.True, $"scenario did not finish in {ScenarioTimeout.TotalSeconds}s: is the backend up on {LocalConnectionRoutes.WsBase}?");
            if (running.IsFaulted)
                ExceptionDispatchInfo.Capture(running.Exception!.InnerException!).Throw();
        }

        private static async Task UntilAsync(Func<bool> condition)
        {
            Stopwatch waited = Stopwatch.StartNew();
            while (!condition())
            {
                if (waited.Elapsed > ScenarioTimeout)
                    throw new TimeoutException($"condition not met in {ScenarioTimeout.TotalSeconds}s: expected the backend to answer on {LocalConnectionRoutes.WsBase}");

                await Task.Yield();
            }
        }

        private static async Task PauseAsync(TimeSpan duration)
        {
            Stopwatch waited = Stopwatch.StartNew();
            while (waited.Elapsed < duration)
                await Task.Yield();
        }
    }
}
#endif
