#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US4-5, US4-6, FR-026, FR-027: recusa tipada e associada ao último comando.</summary>
    public class LiveMatchRefusalTests : LiveMatchTestBase
    {
        private List<PlayRefusal> refusals = null!;

        [SetUp]
        public void RecordRefusals()
        {
            refusals = new List<PlayRefusal>();
            Live.Refused += refusals.Add;
        }

        [Test]
        public async Task RecusaDepoisDeJogadaApontaOComando()
        {
            GoLive(LiveFrames.StartMulligan(4));
            await Live.Commands.PlayUnit(new CardInstanceId(1));

            Rig.Receive(LiveFrames.Refused("not_enough_energy"));

            PlayRefusal refusal = refusals.Single();
            Assert.That((refusal.Code, refusal.Error), Is.EqualTo((PlayRefusalCode.NotEnoughEnergy, "refused by the engine")));
            Assert.That(refusal.ProbableCommand, Is.InstanceOf<PlayUnitCommand>());
            Assert.That(Live.Pending.Current, Is.Null);
            Assert.That(Rig.LogValue("match_play_refused", "probable_command"), Is.EqualTo("play_unit"));
        }

        [Test]
        public void RecusaSemComandoDesdeAUltimaAtualizacaoNaoTemComando()
        {
            GoLive(LiveFrames.StartMulligan(4));

            Rig.Receive(LiveFrames.Refused("not_your_priority"));

            Assert.That(refusals.Single().ProbableCommand, Is.Null);
        }

        [Test]
        public async Task RecusaDepoisDeVoltarAindaApontaOComando()
        {
            GoLive(LiveFrames.StartMulligan(4));
            await Live.Commands.Pass();
            DropAndReopen(TimeSpan.FromSeconds(10));
            Rig.Receive(LiveFrames.Pong);

            Rig.Receive(LiveFrames.Refused("not_your_priority"));

            Assert.That(refusals.Single().ProbableCommand, Is.InstanceOf<PassCommand>());
        }

        [Test]
        public void RecusaNaoMudaEspelhoRelogioNemEstado()
        {
            GoLive(LiveFrames.UpdateAction(5));
            PlayerView view = Live.Mirror.Current!;
            TurnView turn = Live.Clock.Turn!;
            LiveMatchStatus status = Live.Status;

            Rig.Receive(LiveFrames.Refused("match_is_over"));

            Assert.That(Live.Mirror.Current, Is.SameAs(view));
            Assert.That(Live.Clock.Turn, Is.SameAs(turn));
            Assert.That(Live.Status, Is.SameAs(status));
        }
    }
}
