#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US4-3, US4-4, FR-025: a tabela do pendente de contracts/live-match.md.</summary>
    public class PendingPlayTests
    {
        private PendingPlay pending = null!;
        private List<PlayCommand?> changes = null!;
        private PlayCommand play = null!;
        private PlayCommand pass = null!;

        [SetUp]
        public void CreatePending()
        {
            pending = new PendingPlay(new FakeClientLog());
            changes = new List<PlayCommand?>();
            pending.CurrentChanged.Subscribe(changes.Add);
            play = new PlayUnitCommand(new CardInstanceId(12));
            pass = new PassCommand();
        }

        [Test]
        public void EnviadoFicaPendenteEUltimo()
        {
            pending.MarkSent(play);

            Assert.That((pending.Current, pending.LastSentSinceUpdate), Is.EqualTo((play, play)));
            Assert.That(changes, Is.EqualTo(new[] { play }));
        }

        [Test]
        public void NovoEnvioSubstituiSemBloquear()
        {
            pending.MarkSent(play);

            pending.MarkSent(pass);

            Assert.That((pending.Current, pending.LastSentSinceUpdate), Is.EqualTo((pass, pass)));
        }

        [Test]
        public void FalhaNoSocketVoltaAoAnterior()
        {
            pending.MarkSent(play);
            pending.MarkSent(pass);

            pending.Unmark(pass);

            Assert.That(pending.Current, Is.Null);
            Assert.That(pending.LastSentSinceUpdate, Is.SameAs(play));
        }

        [Test]
        public void DesmarcarComandoQueNaoEhOAtualNaoMexe()
        {
            pending.MarkSent(play);
            pending.MarkSent(pass);

            pending.Unmark(play);

            Assert.That((pending.Current, pending.LastSentSinceUpdate), Is.EqualTo((pass, pass)));
        }

        [Test]
        public void AtualizacaoAceitaLimpaOsDois()
        {
            pending.MarkSent(play);

            pending.ClearOnUpdate();

            Assert.That((pending.Current, pending.LastSentSinceUpdate), Is.EqualTo(((PlayCommand?)null, (PlayCommand?)null)));
            Assert.That(changes, Is.EqualTo(new[] { play, null }));
        }

        [Test]
        public void RecusaOuReconexaoLimpamSoOPendente()
        {
            pending.MarkSent(play);

            pending.ClearCurrent();

            Assert.That(pending.Current, Is.Null);
            Assert.That(pending.LastSentSinceUpdate, Is.SameAs(play));
        }

        [Test]
        public void LimparSemPendenteNaoAvisa()
        {
            pending.ClearCurrent();
            pending.ClearOnUpdate();

            Assert.That(changes, Is.Empty);
        }
    }
}
