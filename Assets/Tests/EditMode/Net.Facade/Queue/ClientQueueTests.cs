#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US4-1 a US4-4, FR-005, FR-008: recusa, saída, saída pela conexão e pedido fora de hora.</summary>
    public class ClientQueueTests
    {
        private const string DeckNotFoundFrame = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"deck_id 4 is not a deck of user 7\", \"code\": \"deck_not_found\", \"deck_id\": 4}}";
        private const string MatchmakingFailedFrame = "{\"type\": \"matchmaking_failed\", \"payload\": {\"error\": \"no profile for one of the paired users\"}}";

        private FacadeTestRig rig = null!;
        private List<QueueRefusal> refusals = null!;
        private List<QueueExit> exits = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            refusals = new List<QueueRefusal>();
            exits = new List<QueueExit>();
            rig.Client.Queue.Refused.Subscribe(refusals.Add);
            rig.Client.Queue.Left.Subscribe(exits.Add);
        }

        [Test]
        public async Task DeckInexistenteEhRecusaTipadaEVoltaALogado()
        {
            await rig.SignInAsync();
            rig.JoinQueue();

            rig.Receive(DeckNotFoundFrame);

            Assert.That(refusals.Single().Kind, Is.EqualTo(QueueRefusalKind.DeckNotFound));
            Assert.That((rig.Client.State.Stage, rig.Client.Queue.Phase, exits.Count), Is.EqualTo((ClientStage.SignedIn, QueuePhase.OutOfQueue, 0)));
        }

        [Test]
        public async Task SairProcurandoVoltaALogadoSemRecusa()
        {
            await rig.SignInAsync();
            rig.JoinQueue();

            StageRequestResult result = rig.Client.Queue.Leave();

            Assert.That((result.Applied, result.Stage), Is.EqualTo((true, ClientStage.SignedIn)));
            Assert.That((rig.Client.State.Stage, rig.Client.Queue.Phase), Is.EqualTo((ClientStage.SignedIn, QueuePhase.OutOfQueue)));
            Assert.That((refusals.Count, exits.Count), Is.EqualTo((0, 0)));
        }

        [Test]
        public async Task ConexaoDeFilaDesistindoAvisaSaidaComMotivoEVoltaALogado()
        {
            await rig.SignInAsync();
            rig.Client.Queue.Join(new DeckId(4));
            rig.Pump();

            for (int drop = 0; drop < 6; drop++)
            {
                rig.Sockets.Latest.SimulateClosed(null, "abnormal");
                rig.Advance(TimeSpan.FromSeconds(10));
            }

            QueueExit exit = exits.Single();
            Assert.That((exit.Kind, exit.GiveUp), Is.EqualTo((QueueExitKind.ConnectionGaveUp, GiveUpReason.AttemptsExhausted(5))));
            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.SignedIn));
        }

        [Test]
        public async Task MatchmakingFailedAvisaSaidaEVoltaALogado()
        {
            await rig.SignInAsync();
            rig.JoinQueue();

            rig.Receive(MatchmakingFailedFrame);

            QueueExit exit = exits.Single();
            Assert.That((exit.Kind, exit.GiveUp, exit.ErrorForLog), Is.EqualTo((QueueExitKind.MatchmakingFailed, (GiveUpReason?)null, "no profile for one of the paired users")));
            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.SignedIn));
        }

        [Test]
        public void EntrarDeslogadoNaoSeAplicaENadaEhAberto()
        {
            QueueJoinResult result = rig.Client.Queue.Join(new DeckId(4));

            Assert.That((result.Kind, result.Stage), Is.EqualTo((QueueJoinKind.NotApplicable, ClientStage.SignedOut)));
            Assert.That(rig.Sockets.Created.Count, Is.Zero);
        }

        [Test]
        public async Task EntrarProcurandoJaEstaNaFilaENadaEhEnviadoDeNovo()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            int sent = rig.Sockets.Latest.SentTexts.Count;

            QueueJoinResult result = rig.Client.Queue.Join(new DeckId(5));

            Assert.That((result.Kind, result.Stage), Is.EqualTo((QueueJoinKind.AlreadyQueued, ClientStage.Searching)));
            Assert.That(rig.Sockets.Latest.SentTexts.Count, Is.EqualTo(sent));
        }

        [Test]
        public async Task EntrarEmPartidaNaoSeAplica()
        {
            await rig.ReachInMatchAsync();

            QueueJoinResult result = rig.Client.Queue.Join(new DeckId(4));

            Assert.That((result.Kind, result.Stage), Is.EqualTo((QueueJoinKind.NotApplicable, ClientStage.InMatch)));
        }
    }
}
