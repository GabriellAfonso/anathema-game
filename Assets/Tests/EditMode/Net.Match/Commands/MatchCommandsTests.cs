#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US4-1, US4-2, FR-023, FR-024, SC-007: envio só conectado, pelo socket fake.</summary>
    public class MatchCommandsTests
    {
        private static readonly CardInstanceId Card = new CardInstanceId(12);

        private MatchTestRig rig = null!;
        private PendingPlay pending = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new MatchTestRig();
            pending = new PendingPlay();
        }

        [Test]
        public async Task CadaMetodoMandaOTextoDoContrato()
        {
            MatchCommands commands = new MatchCommands(rig.OpenConnection(), pending, rig.Log);

            await AssertSent(commands.Mulligan(new[] { new CardInstanceId(3), new CardInstanceId(7) }), "{\"type\":\"mulligan\",\"payload\":{\"card_instance_ids\":[3,7]}}");
            await AssertSent(commands.PlayUnit(Card), "{\"type\":\"play_unit\",\"payload\":{\"card_instance_id\":12}}");
            await AssertSent(commands.CastSpell(new CardInstanceId(23)), "{\"type\":\"cast_spell\",\"payload\":{\"card_instance_id\":23}}");
            await AssertSent(commands.CastSpellAt(Card, new CardInstanceId(40)), "{\"type\":\"cast_spell\",\"payload\":{\"card_instance_id\":12,\"target_card_instance_id\":40}}");
            await AssertSent(commands.Pass(), "{\"type\":\"pass\",\"payload\":{}}");
            await AssertSent(commands.DeclareAttack(new[] { new CardInstanceId(21) }), "{\"type\":\"declare_attack\",\"payload\":{\"attacker_card_instance_ids\":[21]}}");
            await AssertSent(commands.WithdrawAttacker(new CardInstanceId(21)), "{\"type\":\"withdraw_attacker\",\"payload\":{\"attacker_card_instance_id\":21}}");
            await AssertSent(commands.ConfirmAttack(), "{\"type\":\"confirm_attack\",\"payload\":{}}");
            await AssertSent(commands.AssignBlocker(new CardInstanceId(4), new CardInstanceId(21)), "{\"type\":\"assign_blocker\",\"payload\":{\"blocker_card_instance_id\":4,\"attacker_card_instance_id\":21}}");
            await AssertSent(commands.RemoveBlocker(new CardInstanceId(4)), "{\"type\":\"remove_blocker\",\"payload\":{\"blocker_card_instance_id\":4}}");
            await AssertSent(commands.EndDefenseWindow(), "{\"type\":\"end_defense_window\",\"payload\":{}}");
            await AssertSent(commands.Forfeit(), "{\"type\":\"forfeit\",\"payload\":{}}");
            Assert.That(rig.PlayTexts().Count, Is.EqualTo(12));
        }

        [Test]
        public async Task SendComComandoMontadoEhIgualAoMetodoNomeado()
        {
            MatchCommands commands = new MatchCommands(rig.OpenConnection(), pending, rig.Log);

            await commands.Send(new PlayUnitCommand(Card));
            await commands.PlayUnit(Card);

            Assert.That(rig.PlayTexts().Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task DesconectadoNaoManda()
        {
            await AssertNotConnected(rig.Connection(), ConnectionPhase.Disconnected);
        }

        [Test]
        public async Task ConectandoNaoManda()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTarget.Match(MatchTestRig.MatchBase, MatchTestRig.Match));

            await AssertNotConnected(connection, ConnectionPhase.Connecting);
        }

        [Test]
        public async Task EsperandoNovaTentativaNaoMandaENadaSaiAoVoltar()
        {
            AuthenticatedConnection connection = rig.OpenConnection();
            rig.Sockets.Latest.SimulateClosed(null, "queda");

            await AssertNotConnected(connection, ConnectionPhase.WaitingRetry);
            rig.Advance(TimeSpan.FromSeconds(10));
            rig.OpenLatest();
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(rig.PlayTexts(), Is.Empty);
        }

        [Test]
        public async Task RenovandoTokenNaoManda()
        {
            AuthenticatedConnection connection = rig.OpenConnection();
            // O fake só segura uma renovação roteirizada; sem roteiro ele lança e a conexão vai esperar nova tentativa.
            rig.Tokens.EnqueueRenewed("token-b");
            rig.Tokens.HoldNextRenewal();
            rig.Receive("{\"type\": \"auth_denied\", \"payload\": {\"error\": \"invalid token\"}}");
            rig.Sockets.Latest.SimulateClosed(4001, "auth_denied");

            await AssertNotConnected(connection, ConnectionPhase.RenewingToken);
        }

        [Test]
        public async Task DesistidaNaoManda()
        {
            AuthenticatedConnection connection = rig.OpenConnection();
            rig.Receive("{\"type\": \"match_denied\", \"payload\": {\"error\": \"no live match\"}}");
            rig.Sockets.Latest.SimulateClosed(4404, "match_denied");

            await AssertNotConnected(connection, ConnectionPhase.GaveUp);
        }

        [Test]
        public async Task FalhaDoSocketDevolveSocketFailedEDesmarca()
        {
            MatchCommands commands = new MatchCommands(rig.OpenConnection(), pending, rig.Log);
            rig.Sockets.Latest.NextSendOutcome = SocketSendOutcome.Failed("broken pipe");

            PlaySendResult result = await commands.Pass();

            Assert.That(result.Status, Is.EqualTo(PlaySendStatus.SocketFailed));
            Assert.That(pending.Current, Is.Null);
            Assert.That(rig.LogValue("match_command_not_sent", "status"), Is.EqualTo("SocketFailed"));
        }

        [Test]
        public async Task EnviadoFicaPendente()
        {
            MatchCommands commands = new MatchCommands(rig.OpenConnection(), pending, rig.Log);

            PlaySendResult result = await commands.PlayUnit(Card);

            Assert.That(pending.Current, Is.SameAs(result.Command));
        }

        private async Task AssertNotConnected(AuthenticatedConnection connection, ConnectionPhase expected)
        {
            MatchCommands commands = new MatchCommands(connection, pending, rig.Log);
            int socketsBefore = rig.Sockets.Created.Count;
            int textsBefore = socketsBefore == 0 ? 0 : rig.PlayTexts().Count;

            PlaySendResult result = await commands.PlayUnit(Card);

            Assert.That(connection.Status.Phase, Is.EqualTo(expected));
            Assert.That((result.Status, result.ConnectionPhase), Is.EqualTo((PlaySendStatus.NotConnected, expected)));
            Assert.That(pending.Current, Is.Null);
            Assert.That(socketsBefore == 0 ? 0 : rig.PlayTexts().Count, Is.EqualTo(textsBefore));
            Assert.That(rig.LogValue("match_command_not_sent", "connection_phase"), Is.EqualTo(expected.ToString()));
        }

        private async Task AssertSent(Task<PlaySendResult> sending, string expected)
        {
            PlaySendResult result = await sending;

            Assert.That(result.Status, Is.EqualTo(PlaySendStatus.Sent));
            Assert.That(rig.PlayTexts().Last(), Is.EqualTo(expected));
        }
    }
}
