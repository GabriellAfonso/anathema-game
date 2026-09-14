#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US6-1 a US6-4, FR-031 a FR-033, FR-037: estados da sessão sobre a conexão.</summary>
    public class LiveMatchConnectionTests : LiveMatchTestBase
    {
        [Test]
        public void StartAbreOAlvoDaPartidaEFicaConectando()
        {
            Live.Start();

            Assert.That(Rig.Sockets.Latest.OpenedUrl!.Query, Does.Contain("matchId=match-7"));
            Assert.That(Live.Status.Phase, Is.EqualTo(LiveMatchPhase.Connecting));
        }

        [Test]
        public void SocketAbertoSemMatchStartContinuaConectando()
        {
            StartAndOpen();

            Assert.That(Connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            Assert.That(Live.Status.Phase, Is.EqualTo(LiveMatchPhase.Connecting));
        }

        [Test]
        public void PrimeiroMatchStartDeixaAoVivoComEspelho()
        {
            GoLive(LiveFrames.StartMulligan(4));

            Assert.That(Live.Status, Is.EqualTo(LiveMatchStatus.Of(LiveMatchPhase.Live)));
            Assert.That(Live.Mirror.Version, Is.EqualTo(4));
            Assert.That(Statuses.Select(status => status.Phase), Is.EqualTo(new[] { LiveMatchPhase.Connecting, LiveMatchPhase.Live }));
        }

        [Test]
        public async Task QuedaMarcaDesatualizadoEVoltaAoVivoNaReconexao()
        {
            GoLive(LiveFrames.StartMulligan(7));

            Rig.Sockets.Latest.SimulateClosed(null, "queda");
            Assert.That((Live.Status.Phase, Live.Status.IsStale), Is.EqualTo((LiveMatchPhase.Reconnecting, true)));
            Assert.That(Live.Mirror.Current, Is.Not.Null);
            Assert.That((await Live.Commands.Pass()).Status, Is.EqualTo(PlaySendStatus.NotConnected));

            Rig.Advance(TimeSpan.FromSeconds(10));
            Rig.OpenLatest();
            Rig.Receive(LiveFrames.StartMulligan(9));

            Assert.That((Live.Status.Phase, Live.Status.IsStale, Live.Mirror.Version), Is.EqualTo((LiveMatchPhase.Live, false, (long?)9)));
        }

        [Test]
        public void MatchDeniedDeixaRecusadaSemNovaAbertura()
        {
            StartAndOpen();

            Rig.Receive(LiveFrames.MatchDenied);
            Rig.Sockets.Latest.SimulateClosed(4404, "match_denied");
            Rig.Advance(TimeSpan.FromSeconds(30));

            Assert.That(Live.Status.Phase, Is.EqualTo(LiveMatchPhase.Refused));
            Assert.That(Live.Status.GiveUp, Is.EqualTo(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound)));
            Assert.That(Rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void SessaoExpiradaDeixaDesistiuComEspelhoLegivel()
        {
            GoLive(LiveFrames.StartMulligan(4));
            Rig.Tokens.EnqueueRenewalExpired();

            Rig.Receive(LiveFrames.AuthDenied);
            Rig.Sockets.Latest.SimulateClosed(4001, "auth_denied");

            Assert.That((Live.Status.Phase, Live.Status.IsStale), Is.EqualTo((LiveMatchPhase.GaveUp, true)));
            Assert.That(Live.Status.GiveUp, Is.EqualTo(GiveUpReason.SessionExpired()));
            Assert.That(Live.Mirror.Current, Is.Not.Null);
        }

        [Test]
        public void SegundoStartLanca()
        {
            Live.Start();

            Assert.Throws<InvalidOperationException>(() => Live.Start());
        }

        [Test]
        public void CadaMudancaDeEstadoEhRegistrada()
        {
            GoLive(LiveFrames.StartMulligan(4));

            Assert.That(Rig.Log.Entries.Count(entry => entry.EventName == "match_status"), Is.EqualTo(2));
        }

        [Test]
        public void StatsOfUsaOCatalogoDaSessao()
        {
            GoLive(LiveFrames.StartAction(5));

            DisplayedUnitStats stats = Live.StatsOf(Live.Mirror.Current!.You.Bank[0])!;

            Assert.That((stats.Attack, stats.Health), Is.EqualTo((2L, 3L)));
        }

        [Test]
        public void HintForAntesDoPrimeiroEstadoEhForaDaMao()
        {
            Assert.That(Live.HintFor(new CardInstanceId(1)).Kind, Is.EqualTo(HintCardKind.NotInHand));
        }

        [Test]
        public void HintForUsaAVisaoAtual()
        {
            GoLive(LiveFrames.StartMulligan(4));

            HandCardHint hint = Live.HintFor(new CardInstanceId(3));

            Assert.That((hint.Kind, hint.Target), Is.EqualTo((HintCardKind.Spell, (Anathema.Net.Account.SpellTargetKind?)Anathema.Net.Account.SpellTargetKind.EnemyUnit)));
        }
    }
}
