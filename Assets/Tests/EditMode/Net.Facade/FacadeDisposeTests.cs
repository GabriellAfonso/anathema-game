#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>spec, casos de borda: descartar a fachada fecha fila e partida de propósito, cancela e cala todos os avisos.</summary>
    public class FacadeDisposeTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task DescartarFechaOsSocketsENadaEntregaDepois()
        {
            await rig.ReachInMatchAsync();
            LiveMatch match = rig.Client.CurrentMatch!;
            int delivered = 0;
            rig.Client.StageChanged.Subscribe(_ => delivered++);
            match.Mirror.ViewReplaced.Subscribe(_ => delivered++);
            match.StatusChanged.Subscribe(_ => delivered++);

            rig.Client.Dispose();
            rig.Receive(FacadeTestRig.MatchFixture("contract-match-update-action.json"));
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(delivered, Is.Zero);
            Assert.That(rig.Sockets.Created.All(socket => socket.CloseRequests > 0), Is.True, "todo socket foi fechado de propósito");
        }

        [Test]
        public async Task DescartarNoFimCancelaABuscaDaLinha()
        {
            await rig.ReachFinishedAsync(FacadeTestRig.HistoryPage(withRow: false));
            int updates = 0;
            rig.Client.ResultUpdated.Subscribe(_ => updates++);

            rig.Client.Dispose();
            rig.Advance(TimeSpan.FromSeconds(10));

            Assert.That((updates, rig.HistoryReads()), Is.EqualTo((0, 1)));
        }
    }
}
