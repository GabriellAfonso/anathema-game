#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;

namespace Anathema.Net.Match.Tests
{
    /// <summary>
    /// Conexão de partida da 003 sobre os fakes, com o codec real da partida e o catálogo de teste. A
    /// montagem repete a do <c>ConnectionTestRig</c>, que é interno de outra assembly de teste
    /// (specs/004-match-session/plan.md, Complexity Tracking).
    /// </summary>
    internal sealed class MatchTestRig
    {
        internal const string MatchBaseText = "ws://127.0.0.1:8000/ws/match/";
        internal const string MatchIdText = "match-7";

        internal MatchTestRig()
        {
            Lifecycle = new FakeAppLifecycle(Clock);
            Queue = new MainThreadQueue(Log);
            Tokens = new FakeAccessTokenSource(Clock);
            Tokens.EnqueueValid("token-a");
            Codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), Log);
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

        internal LoadedCatalog Catalog { get; } = MatchTestCatalog.Default();

        internal static Uri MatchBase => new Uri(MatchBaseText);

        internal static MatchId Match => new MatchId(MatchIdText);

        internal AuthenticatedConnection Connection()
        {
            ReconnectPolicy policy = new ReconnectPolicy(baseDelaySeconds: 0.5, maxDelaySeconds: 5.0, maxAttempts: int.MaxValue, random: () => 0.5);
            return new AuthenticatedConnection(Ports, Tokens, Codec, new ConnectionSettings(policy, ConnectionTiming.Default));
        }

        internal FakeWebSocket OpenLatest()
        {
            FakeWebSocket socket = Sockets.Latest;
            socket.SimulateOpened();
            return socket;
        }

        internal void Receive(string frameJson) => Sockets.Latest.SimulateText(frameJson);

        internal void ReceiveFixture(string name) => Receive(MatchFixtures.Text(name));

        internal ServerFrame Decode(string frameJson) => Codec.Decode(frameJson).Value;

        internal ServerFrame DecodeFixture(string name) => Decode(MatchFixtures.Text(name));

        internal void Advance(TimeSpan duration)
        {
            Clock.Advance(duration);
            Ticker.Tick();
        }

        internal IReadOnlyList<string> SentTexts() => Sockets.Latest.SentTexts;

        // A conexão manda ping ao abrir e no intervalo; os testes de jogada olham só as outras mensagens.
        internal IReadOnlyList<string> PlayTexts() => Sockets.Latest.SentTexts.Where(text => !text.Contains("\"type\":\"ping\"")).ToArray();

        internal AuthenticatedConnection OpenConnection()
        {
            AuthenticatedConnection connection = Connection();
            connection.Connect(ConnectionTarget.Match(MatchBase, Match));
            OpenLatest();
            return connection;
        }

        internal string LogValue(string eventName, string field)
        {
            foreach (LogField logField in Log.Single(eventName).Fields)
            {
                if (logField.Name == field)
                    return logField.Value;
            }

            throw new InvalidOperationException($"log event '{eventName}' has no field '{field}': expected the fields of contracts/live-match.md");
        }
    }
}
