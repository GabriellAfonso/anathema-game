#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US2-3, US2-4, US3-5, US3-6, FR-010, FR-021, SC-005, SC-006: regra de versão e relógio pela sessão.</summary>
    public class LiveMatchVersionTests : LiveMatchTestBase
    {
        [Test]
        public void UpdateMenorOuIgualEhDescartadoSemAviso()
        {
            GoLive(LiveFrames.StartMulligan(7));
            int replaced = 0;
            Live.Mirror.ViewReplaced += _ => replaced++;

            Rig.Receive(LiveFrames.UpdateAction(6));
            Rig.Receive(LiveFrames.UpdateAction(7));

            Assert.That((replaced, Live.Mirror.Version, Live.Mirror.Phase), Is.EqualTo((0, (long?)7, (MatchPhase?)MatchPhase.Mulligan)));
            Assert.That(Rig.Log.Entries.Count(entry => entry.EventName == "match_frame_discarded"), Is.EqualTo(2));
        }

        [Test]
        public void MatchStartDeMesmaVersaoNaoTrocaEMaiorTroca()
        {
            GoLive(LiveFrames.StartMulligan(7));
            PlayerView before = Live.Mirror.Current!;
            List<long?> replacedVersions = new List<long?>();
            Live.Mirror.ViewReplaced += _ => replacedVersions.Add(Live.Mirror.Version);

            DropAndReopen(TimeSpan.FromSeconds(10));
            Rig.Receive(LiveFrames.StartMulligan(7));
            Assert.That((Live.Mirror.Current, replacedVersions.Count, Live.Status.Phase), Is.EqualTo((before, 0, LiveMatchPhase.Live)));

            Rig.Receive(LiveFrames.StartMulligan(9));
            Assert.That(replacedVersions, Is.EqualTo(new long?[] { 9 }));
        }

        [Test]
        public void FrameDescartadoNaoMexeNoRelogio()
        {
            GoLive(LiveFrames.UpdateAction(7, 25000));

            Rig.Receive(LiveFrames.UpdateAction(6, 1000));

            Assert.That(Live.Clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(25000)));
        }

        [Test]
        public void MatchStartDeReconexaoComMesmaVersaoReancoraORelogio()
        {
            GoLive(LiveFrames.UpdateAction(7, 25000));
            PlayerView before = Live.Mirror.Current!;

            DropAndReopen(TimeSpan.FromSeconds(8));
            Rig.Receive(LiveFrames.StartAction(7, 17000));

            Assert.That(Live.Mirror.Current, Is.SameAs(before));
            Assert.That(Live.Clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(17000)));
            Rig.Clock.Advance(TimeSpan.FromSeconds(2));
            Assert.That(Live.Clock.TurnRemaining, Is.EqualTo(TimeSpan.FromMilliseconds(15000)));
        }

        [Test]
        public void BuracoDeVersaoPorTurnWarningEhAceito()
        {
            GoLive(LiveFrames.UpdateAction(7));

            Rig.Receive(LiveFrames.WarningFor(12));
            Rig.Receive(LiveFrames.UpdateAction(9));

            Assert.That(Live.Mirror.Version, Is.EqualTo(9));
        }

        [Test]
        public void RelogioNovoJaValeDentroDoAvisoDeEstadoEVezNovaSaiDepois()
        {
            StartAndOpen();
            List<string> notices = new List<string>();
            Live.Mirror.ViewReplaced += _ => notices.Add("view:turn=" + Live.Clock.Turn?.TurnNumber);
            Live.Mirror.EventReceived += item => notices.Add("event:" + item.KindText);
            Live.Clock.TurnStarted += turn => notices.Add("turn_started:" + turn.TurnNumber);

            Rig.Receive(LiveFrames.UpdateAction(5));

            Assert.That(notices.First(), Is.EqualTo("view:turn=12"));
            Assert.That(notices.Last(), Is.EqualTo("turn_started:12"));
            Assert.That(notices.Count(notice => notice.StartsWith("event:", StringComparison.Ordinal)), Is.EqualTo(4));
        }

        [Test]
        public async Task AtualizacaoAceitaLimpaPendenteEUltimoEnviado()
        {
            GoLive(LiveFrames.StartMulligan(4));
            await Live.Commands.PlayUnit(new CardInstanceId(1));

            Rig.Receive(LiveFrames.UpdateAction(8));

            Assert.That((Live.Pending.Current, Live.Pending.LastSentSinceUpdate), Is.EqualTo(((PlayCommand?)null, (PlayCommand?)null)));
        }

        [Test]
        public void SequenciaEmbaralhadaNuncaExpoeVersaoMenor()
        {
            StartAndOpen();
            List<long> exposed = new List<long>();
            Live.Mirror.ViewReplaced += _ => exposed.Add(Live.Mirror.Version!.Value);

            foreach (long version in new long[] { 5, 3, 9, 9, 7, 12, 1, 12, 13 })
                Rig.Receive(LiveFrames.UpdateAction(version));

            Assert.That(exposed, Is.EqualTo(new long[] { 5, 9, 12, 13 }));
        }
    }
}
