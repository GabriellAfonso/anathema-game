#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US6-5, FR-034, FR-035, FR-025: fim de partida, saída de propósito e pendente na reconexão.</summary>
    public class LiveMatchFinishTests : LiveMatchTestBase
    {
        [Test]
        public void FaseTerminadaPublicaDesfechoEFechaOSocketDePropósito()
        {
            GoLive(LiveFrames.StartMulligan(4));
            MatchEnding? ending = null;
            Live.Mirror.MatchEnded += value => ending = value;

            Rig.Receive(LiveFrames.UpdateFinished(20));
            Rig.Advance(TimeSpan.FromSeconds(30));

            Assert.That(ending!.Won, Is.True);
            Assert.That(Live.Status.Phase, Is.EqualTo(LiveMatchPhase.Finished));
            Assert.That(Live.Status.Outcome!.DefeatedUser, Is.EqualTo(new UserId(9)));
            Assert.That(Rig.Sockets.Latest.CloseRequests, Is.EqualTo(1));
            Assert.That(Rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(Statuses.Select(status => status.Phase), Has.None.EqualTo(LiveMatchPhase.Reconnecting));
        }

        [Test]
        public void MatchStartJaTerminadoDepoisDeQuedaTerminaDireto()
        {
            GoLive(LiveFrames.StartMulligan(9));

            DropAndReopen(TimeSpan.FromSeconds(10));
            Rig.Receive(LiveFrames.StartFinished(20));

            Assert.That(Live.Status.Phase, Is.EqualTo(LiveMatchPhase.Finished));
            Assert.That(Rig.Sockets.Latest.CloseRequests, Is.EqualTo(1));
        }

        [Test]
        public void DisposeFechaOSocketECalaAvisos()
        {
            GoLive(LiveFrames.StartMulligan(4));
            int statusesBefore = Statuses.Count;
            int replaced = 0;
            Live.Mirror.ViewReplaced += _ => replaced++;

            Live.Dispose();

            Assert.That(Rig.Sockets.Latest.CloseRequests, Is.EqualTo(1));
            Assert.That(Statuses.Count, Is.EqualTo(statusesBefore));
            Assert.That(replaced, Is.EqualTo(0));
            Assert.That(Rig.Log.Entries.Count(entry => entry.EventName == "match_event"), Is.EqualTo(0));
        }

        [Test]
        public void DisposeSemStartNaoTocaNaConexao()
        {
            Live.Dispose();

            Assert.That(Rig.Sockets.Created, Is.Empty);
        }

        [Test]
        public async Task VoltarDepoisDeCairLimpaOPendente()
        {
            GoLive(LiveFrames.StartMulligan(4));
            await Live.Commands.PlayUnit(new CardInstanceId(1));

            DropAndReopen(TimeSpan.FromSeconds(10));
            Rig.Receive(LiveFrames.Pong);

            Assert.That(Live.Pending.Current, Is.Null);
            Assert.That(Live.Pending.LastSentSinceUpdate, Is.InstanceOf<PlayUnitCommand>());
        }
    }
}
