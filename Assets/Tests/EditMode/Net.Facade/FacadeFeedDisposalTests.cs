#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>
    /// FR-020, SC-004: cada aviso da fachada, da partida, do espelho, do relógio e do pendente não entrega nada depois
    /// de descartado, num roteiro em que todos esses avisos saem.
    /// </summary>
    public class FacadeFeedDisposalTests
    {
        private const string Pong = "{\"type\": \"pong\", \"payload\": {}}";
        private const string NotYourPriority = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"not your priority\", \"code\": \"not_your_priority\"}}";
        private const string DeckNotFound = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"no deck\", \"code\": \"deck_not_found\", \"deck_id\": 4}}";
        private const string MatchmakingFailed = "{\"type\": \"matchmaking_failed\", \"payload\": {\"error\": \"no profile\"}}";

        private FacadeTestRig rig = null!;
        private int delivered;
        private int control;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            delivered = 0;
            control = 0;
        }

        [Test]
        public async Task AvisosDaFachadaEDaPartidaDescartadosNaoEntregam()
        {
            DropFacadeFeeds();
            rig.Client.StageChanged.Subscribe(_ => control++);
            await rig.ReachInMatchAsync();
            DropMatchFeeds(rig.Client.CurrentMatch!);

            PlayWholeMatch();

            Assert.That(control, Is.GreaterThan(0), "o roteiro precisa ter publicado avisos");
            Assert.That(delivered, Is.Zero);
        }

        [Test]
        public async Task AvisosDeRecusaESaidaDaFilaDescartadosNaoEntregam()
        {
            Drop(rig.Client.Queue.Refused);
            Drop(rig.Client.Queue.Left);
            rig.Client.Queue.Refused.Subscribe(_ => control++);
            await rig.SignInAsync();

            rig.JoinQueue();
            rig.Receive(DeckNotFound);
            // A conexão de fila continua aberta depois da recusa: a segunda entrada manda pelo mesmo socket.
            rig.Client.Queue.Join(new Anathema.Net.Core.DeckId(4));
            rig.Pump();
            rig.Receive(MatchmakingFailed);

            Assert.That((control, delivered), Is.EqualTo((1, 0)));
        }

        private void DropFacadeFeeds()
        {
            Drop(rig.Client.StageChanged);
            Drop(rig.Client.ResultUpdated);
            Drop(rig.Client.Queue.Paired);
            Drop(rig.Client.Health.Reconnecting);
            Drop(rig.Client.Health.Recovered);
            Drop(rig.Client.Health.GaveUp);
            Drop(rig.Client.Health.LatencyMeasured);
        }

        private void DropMatchFeeds(LiveMatch match)
        {
            Drop(match.StatusChanged);
            Drop(match.Refused);
            Drop(match.Mirror.ViewReplaced);
            Drop(match.Mirror.EventReceived);
            Drop(match.Mirror.PhaseChanged);
            Drop(match.Mirror.PriorityChanged);
            Drop(match.Mirror.MatchEnded);
            Drop(match.Clock.TurnStarted);
            Drop(match.Clock.TurnRunningOut);
            Drop(match.Pending.CurrentChanged);
        }

        private void PlayWholeMatch()
        {
            rig.Receive(Pong);
            rig.ReceiveMatchFixture("contract-match-update-action.json");
            _ = rig.Client.CurrentMatch!.Commands.Pass();
            rig.Receive(NotYourPriority);
            rig.ReceiveMatchFixture("contract-turn-warning.json");
            rig.DropMatchSocketAndReopen();
            rig.Receive(Pong);
            rig.Http.RespondNext(200, FacadeTestRig.HistoryPage(withRow: true));
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);
        }

        private void Drop<T>(Anathema.Net.Core.EventFeed<T> feed)
        {
            IDisposable subscription = feed.Subscribe(_ => delivered++);
            subscription.Dispose();
        }
    }
}
