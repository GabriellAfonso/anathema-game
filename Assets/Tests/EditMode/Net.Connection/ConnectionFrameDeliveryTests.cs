#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US1-8: entrega em ordem, texto inválido, envio, e token fora do log (FR-016).</summary>
    public class ConnectionFrameDeliveryTests
    {
        private ConnectionTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
        }

        [Test]
        public void FramesChegamNaOrdemETextoInvalidoEhDescartado()
        {
            AuthenticatedConnection connection = ConnectAndOpen();
            List<ServerFrame> frames = new List<ServerFrame>();
            connection.FrameReceived += frames.Add;

            rig.Receive(TestFrames.Pong);
            rig.Receive("isto não é json");
            rig.Receive(TestFrames.Unknown("match_update"));
            rig.Receive(TestFrames.Refused("deck_not_found"));

            Assert.That(frames.Select(frame => frame.GetType()), Is.EqualTo(new[] { typeof(PongFrame), typeof(UnknownServerFrame), typeof(MessageRefusedFrame) }));
            Assert.That(rig.LogValue("connection_frame_invalid", "kind"), Is.EqualTo(nameof(DecodeFailureKind.NotJson)));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
        }

        [Test]
        public void TextoCruAcompanhaCadaFrameAceito()
        {
            AuthenticatedConnection connection = ConnectAndOpen();
            List<string> texts = new List<string>();
            connection.RawTextReceived += texts.Add;

            rig.Receive(TestFrames.Pong);
            rig.Receive("[]");

            Assert.That(texts, Is.EqualTo(new[] { TestFrames.Pong }));
        }

        [Test]
        public async Task EnviarSemSocketAbertoDevolveNotOpenERegistra()
        {
            AuthenticatedConnection connection = rig.Connection();

            SocketSendOutcome outcome = await connection.SendAsync(new PingMessage(null));

            Assert.That(outcome, Is.SameAs(SocketSendOutcome.NotOpen));
            Assert.That(rig.LogValue("connection_send_not_open", "message_type"), Is.EqualTo(PingMessage.TypeName));
        }

        [Test]
        public async Task EnviarComSocketAbertoCodificaAMensagem()
        {
            AuthenticatedConnection connection = ConnectAndOpen();

            SocketSendOutcome outcome = await connection.SendAsync(new PingMessage(null));

            Assert.That(outcome.Status, Is.EqualTo(SocketSendStatus.Sent));
            Assert.That(rig.Sockets.Latest.SentTexts.Last(), Does.Contain("\"type\":\"ping\""));
        }

        [Test]
        public async Task TokenNuncaApareceNoLog()
        {
            rig.Tokens.EnqueueValid("secret-token-A");
            rig.Tokens.EnqueueRenewed("secret-token-B");
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchTarget());
            rig.RefuseLatestToken();
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            rig.Advance(TimeSpan.FromSeconds(5));
            connection.Leave();
            await connection.SendAsync(new PingMessage(null));

            IEnumerable<string> logged = rig.Log.Entries.SelectMany(entry => entry.Fields.Select(field => field.Value).Append(entry.EventName));
            Assert.That(logged, Has.None.Contains("secret-token"));
            Assert.That(rig.Log.Entries.Count, Is.GreaterThan(5));
        }

        private AuthenticatedConnection ConnectAndOpen()
        {
            rig.Tokens.EnqueueValid("token-A");
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.OpenLatest();
            return connection;
        }
    }
}
