#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>research R4: um dono das transições, que troca inteiro, registra e avisa uma vez.</summary>
    public class ClientStagesTests
    {
        private static readonly UserId Self = new UserId(7);

        private FakeClientLog log = null!;
        private ClientStages stages = null!;
        private List<ClientStageChange> changes = null!;

        [SetUp]
        public void CreateStages()
        {
            log = new FakeClientLog();
            stages = new ClientStages(log);
            changes = new List<ClientStageChange>();
            stages.StageChanged.Subscribe(changes.Add);
        }

        [Test]
        public void ComecaDeslogadoNaAberturaNaGeracaoZero()
        {
            Assert.That((stages.State.Stage, stages.State.SignedOutReason, stages.Generation), Is.EqualTo((ClientStage.SignedOut, (SignedOutReason?)SignedOutReason.Startup, 0)));
        }

        [Test]
        public void MoverTrocaOEstadoAvisaUmaVezERegistra()
        {
            ClientState signedIn = ClientState.SignedIn(Self);

            bool moved = stages.Move(signedIn, "signed_in");

            Assert.That(moved, Is.True);
            Assert.That(stages.State, Is.SameAs(signedIn));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That((changes[0].Previous.Stage, changes[0].Current), Is.EqualTo((ClientStage.SignedOut, signedIn)));
            ClientLogEntry entry = log.Single("client_stage");
            Assert.That(entry.Fields, Has.Some.Matches<LogField>(field => field.Name == "cause" && field.Value == "signed_in"));
        }

        [Test]
        public void MoverParaOMesmoEstadoNaoAvisa()
        {
            stages.Move(ClientState.SignedIn(Self), "signed_in");

            bool moved = stages.Move(ClientState.SignedIn(Self), "resumed");

            Assert.That(moved, Is.False);
            Assert.That(changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TrocarNoMesmoEstagioNaoAvisaEOutroEstagioLanca()
        {
            stages.Move(ClientState.SignedIn(Self), "signed_in");
            ClientState again = ClientState.SignedIn(Self);

            stages.Replace(again);

            Assert.That((stages.State, changes.Count), Is.EqualTo((again, 1)));
            Assert.Throws<InvalidOperationException>(() => stages.Replace(ClientState.Searching(Self)));
        }

        [Test]
        public void InvalidarSobeAGeracao()
        {
            stages.Invalidate();
            stages.Invalidate();

            Assert.That(stages.Generation, Is.EqualTo(2));
        }

        [Test]
        public void EstadoNuloLanca()
        {
            Assert.Throws<ArgumentNullException>(() => stages.Move(null!, "none"));
        }
    }
}
