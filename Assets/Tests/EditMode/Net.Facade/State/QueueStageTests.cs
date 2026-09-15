#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US1-3, FR-010, FR-011: entrar na fila leva a procurando; o pareamento leva a pareado com os dois jogadores.</summary>
    public class QueueStageTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task EntrarNaFilaLevaAProcurandoPelaRotaDeFila()
        {
            await rig.SignInAsync();

            QueueJoinResult result = rig.Client.Queue.Join(new DeckId(4));
            rig.Pump();

            Assert.That((result.Kind, result.Stage), Is.EqualTo((QueueJoinKind.Started, ClientStage.Searching)));
            Assert.That((rig.Client.State.Stage, rig.Client.State.Self), Is.EqualTo((ClientStage.Searching, (UserId?)new UserId(7))));
            Assert.That(rig.Sockets.Latest.OpenedUrl!.AbsolutePath, Is.EqualTo("/ws/matchmaking/"));
        }

        [Test]
        public async Task PareamentoLevaAPareadoComOsDoisJogadores()
        {
            await rig.SignInAsync();
            rig.JoinQueue();
            List<ClientStageChange> changes = new List<ClientStageChange>();
            List<MatchPairing> pairings = new List<MatchPairing>();
            rig.Client.StageChanged.Subscribe(changes.Add);
            rig.Client.Queue.Paired.Subscribe(pairings.Add);

            rig.ReceiveMatchFound();

            MatchPairing pairing = rig.Client.State.Pairing!;
            Assert.That(rig.Client.State.Stage, Is.EqualTo(ClientStage.Paired));
            Assert.That((pairing.Match.Value, pairing.Self.Nickname, pairing.Opponent.User), Is.EqualTo((FacadeTestRig.MatchIdText, "one", new UserId(9))));
            Assert.That(pairings, Is.EqualTo(new[] { pairing }));
            Assert.That((changes.Count, changes[0].Current.Pairing), Is.EqualTo((1, pairing)));
        }
    }
}
