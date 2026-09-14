#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class HeartbeatTests
    {
        private static Heartbeat Beat(double interval = 10.0, double timeout = 30.0)
        {
            return new Heartbeat(intervalSeconds: interval, timeoutSeconds: timeout);
        }

        /// <summary>Avanca o relogio em passos e devolve a ultima acao vista.</summary>
        private static HeartbeatAction Advance(Heartbeat beat, double seconds, double step = 1.0)
        {
            HeartbeatAction last = HeartbeatAction.Idle;

            for (double elapsed = 0.0; elapsed < seconds; elapsed += step)
            {
                HeartbeatAction action = beat.Tick(step);

                if (action != HeartbeatAction.Idle)
                    last = action;
            }

            return last;
        }

        // ---------- ping ----------

        [Test]
        public void NaoPingaAntesDoIntervalo()
        {
            Heartbeat beat = Beat(interval: 10);

            Assert.AreEqual(HeartbeatAction.Idle, Advance(beat, 9));
        }

        [Test]
        public void PingaAoCompletarOIntervalo()
        {
            Heartbeat beat = Beat(interval: 10);

            Assert.AreEqual(HeartbeatAction.SendPing, beat.Tick(10));
        }

        [Test]
        public void PingaDeNovoACadaIntervalo()
        {
            Heartbeat beat = Beat(interval: 10);

            Assert.AreEqual(HeartbeatAction.SendPing, beat.Tick(10));
            Assert.AreEqual(HeartbeatAction.Idle, beat.Tick(9));
            Assert.AreEqual(HeartbeatAction.SendPing, beat.Tick(1));
        }

        // ---------- timeout so depois do primeiro pong ----------

        [Test]
        public void SemPongNuncaDeclaraMorte()
        {
            // Servidor que ainda nao responde ping: o cliente pinga no vazio, mas
            // nunca derruba uma conexao que pode estar boa.
            Heartbeat beat = Beat(interval: 10, timeout: 30);

            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual(HeartbeatAction.DeclareDead, beat.Tick(1));
        }

        [Test]
        public void DepoisDoPongOSilencioLongoDeclaraMorte()
        {
            Heartbeat beat = Beat(interval: 10, timeout: 30);
            beat.NotePong();

            Assert.AreEqual(HeartbeatAction.DeclareDead, Advance(beat, 31));
        }

        [Test]
        public void SilencioCurtoNaoDeclaraMorte()
        {
            Heartbeat beat = Beat(interval: 10, timeout: 30);
            beat.NotePong();

            Assert.AreNotEqual(HeartbeatAction.DeclareDead, Advance(beat, 29));
        }

        // ---------- prova de vida ----------

        [Test]
        public void QualquerMensagemAdiaOTimeout()
        {
            // Nao so o pong: trafego de jogo tambem prova que o cano esta vivo.
            Heartbeat beat = Beat(interval: 10, timeout: 30);
            beat.NotePong();

            Advance(beat, 25);
            beat.NoteInbound();

            Assert.AreNotEqual(HeartbeatAction.DeclareDead, Advance(beat, 25));
        }

        [Test]
        public void SilenceSecondsContaDesdeAUltimaMensagem()
        {
            Heartbeat beat = Beat();

            beat.Tick(5);
            Assert.AreEqual(5, beat.SilenceSeconds, 1e-9);

            beat.NoteInbound();
            Assert.AreEqual(0, beat.SilenceSeconds, 1e-9);
        }

        // ---------- reset ----------

        [Test]
        public void ResetDesligaOTimeoutDeNovo()
        {
            // Socket novo comeca sem contrato provado: se o servidor mudar de
            // versao, o cliente nao herda a suposicao do socket anterior.
            Heartbeat beat = Beat(interval: 10, timeout: 30);
            beat.NotePong();

            beat.Reset();

            Assert.AreEqual(HeartbeatAction.Idle, Advance(beat, 5));
            for (int i = 0; i < 100; i++)
                Assert.AreNotEqual(HeartbeatAction.DeclareDead, beat.Tick(1));
        }

        // ---------- validacao ----------

        [Test]
        public void ArgumentosInvalidosSaoRejeitadosNaConstrucao()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Heartbeat(intervalSeconds: 0));

            // Timeout menor que o intervalo derrubaria a conexao antes do primeiro
            // pong ter chance de voltar.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Heartbeat(intervalSeconds: 10, timeoutSeconds: 10));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Heartbeat(intervalSeconds: 10, timeoutSeconds: 5));
        }

        // ---------- pausa e ping fora do ciclo (feature 003) ----------

        [Test]
        public void ForgivePauseZeraOSilencioEMantemOTimeoutArmado()
        {
            Heartbeat beat = Beat(interval: 10, timeout: 30);
            beat.NotePong();
            Advance(beat, 25);

            beat.ForgivePause();

            Assert.AreEqual(0, beat.SilenceSeconds, 1e-9);
            Assert.AreNotEqual(HeartbeatAction.DeclareDead, Advance(beat, 29));
            Assert.AreEqual(HeartbeatAction.DeclareDead, Advance(beat, 2));
        }

        [Test]
        public void NotePingSentAdiaOProximoPing()
        {
            Heartbeat beat = Beat(interval: 10);
            beat.Tick(9);

            beat.NotePingSent();

            Assert.AreEqual(HeartbeatAction.Idle, beat.Tick(9));
            Assert.AreEqual(HeartbeatAction.SendPing, beat.Tick(1));
        }
    }
}
