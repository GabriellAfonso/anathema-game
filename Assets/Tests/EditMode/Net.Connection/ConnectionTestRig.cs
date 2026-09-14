#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>
    /// Portas falsas e codec real de uma conexão sob teste. Criar no <c>[SetUp]</c>: o NUnit reaproveita
    /// a instância do fixture, e fakes em campo inicializado vazariam de um teste para o outro.
    /// </summary>
    internal sealed class ConnectionTestRig
    {
        internal const string MatchmakingBase = "ws://127.0.0.1:8000/ws/matchmaking/";
        internal const string MatchBase = "ws://127.0.0.1:8000/ws/match/";

        internal ConnectionTestRig()
        {
            Lifecycle = new FakeAppLifecycle(Clock);
            Queue = new MainThreadQueue(Log);
            Tokens = new FakeAccessTokenSource(Clock);
            Codec = ConnectionTestCodec.Codec(Log);
            Ports = new ConnectionPorts(Sockets, Clock, Ticker, Lifecycle, Reachability, Queue, Log);
        }

        internal FakeWebSocketFactory Sockets { get; } = new FakeWebSocketFactory();

        internal FakeMonotonicClock Clock { get; } = new FakeMonotonicClock();

        internal FakeFrameTicker Ticker { get; } = new FakeFrameTicker();

        internal FakeNetworkReachability Reachability { get; } = new FakeNetworkReachability(NetworkKind.LocalArea);

        internal FakeClientLog Log { get; } = new FakeClientLog();

        internal FakeAppLifecycle Lifecycle { get; }

        internal MainThreadQueue Queue { get; }

        internal FakeAccessTokenSource Tokens { get; }

        internal IProtocolCodec Codec { get; }

        internal ConnectionPorts Ports { get; }

        internal static ConnectionTarget MatchmakingTarget => ConnectionTarget.Matchmaking(new Uri(MatchmakingBase));

        internal static ConnectionTarget MatchTarget(string matchId = "match-1") => ConnectionTarget.Match(new Uri(MatchBase), new MatchId(matchId));

        /// <summary>Política sem jitter efetivo (random 0,5 dá fator 1): esperas 0,5 s, 1 s, 2 s…, teto 5 s.</summary>
        internal static ConnectionSettings Deterministic(int maxAttempts = int.MaxValue)
        {
            return new ConnectionSettings(new ReconnectPolicy(baseDelaySeconds: 0.5, maxDelaySeconds: 5.0, maxAttempts: maxAttempts, random: () => 0.5), ConnectionTiming.Default);
        }

        internal AuthenticatedConnection Connection(ConnectionSettings? settings = null)
        {
            return new AuthenticatedConnection(Ports, Tokens, Codec, settings ?? Deterministic());
        }

        internal MatchQueue QueueOver(AuthenticatedConnection connection)
        {
            return new MatchQueue(connection, MatchmakingTarget, Log);
        }

        internal static List<ConnectionPhase> RecordPhases(AuthenticatedConnection connection)
        {
            List<ConnectionPhase> phases = new List<ConnectionPhase>();
            connection.StatusChanged += status => phases.Add(status.Phase);
            return phases;
        }

        /// <summary>O servidor recusa o token do socket da vez: abre, manda auth_denied, fecha com 4001.</summary>
        internal void RefuseLatestToken()
        {
            FakeWebSocket socket = OpenLatest();
            socket.SimulateText(TestFrames.AuthDenied);
            socket.SimulateClosed(4001, "auth_denied");
        }

        /// <summary>Os join_queue mandados pelo socket da vez, sem os pings do heartbeat.</summary>
        internal List<string> JoinTexts()
        {
            return Sockets.Latest.SentTexts.Where(text => text.Contains("\"type\":\"join_queue\"")).ToList();
        }

        internal string LogValue(string eventName, string field)
        {
            foreach (LogField logField in Log.Single(eventName).Fields)
            {
                if (logField.Name == field)
                    return logField.Value;
            }

            throw new InvalidOperationException($"log event '{eventName}' has no field '{field}': expected the fields of contracts/authenticated-connection.md");
        }

        /// <summary>Um quadro depois de <paramref name="duration"/>: avança o relógio e chama o ticker.</summary>
        internal void Advance(TimeSpan duration)
        {
            Clock.Advance(duration);
            Ticker.Tick();
        }

        /// <summary>Quadros de um segundo até somar <paramref name="duration"/>.</summary>
        internal void AdvanceInSeconds(TimeSpan duration)
        {
            for (TimeSpan elapsed = TimeSpan.Zero; elapsed < duration; elapsed += TimeSpan.FromSeconds(1))
                Advance(TimeSpan.FromSeconds(1));
        }

        /// <summary>O handshake do socket da vez termina.</summary>
        internal FakeWebSocket OpenLatest()
        {
            FakeWebSocket socket = Sockets.Latest;
            socket.SimulateOpened();
            return socket;
        }

        /// <summary>O servidor manda um frame pelo socket da vez.</summary>
        internal void Receive(string frameJson) => Sockets.Latest.SimulateText(frameJson);

        /// <summary>Roda o que a conexão deixou na fila da thread principal.</summary>
        internal void Drain() => Queue.Drain();
    }
}
