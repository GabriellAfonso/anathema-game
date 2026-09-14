#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Detecta conexao morta que o sistema operacional nao reporta.
    ///
    /// TCP meio-aberto -- wifi que cai, cabo arrancado, NAT que expira -- nao gera
    /// close nenhum. Sem isto o socket fica pendurado em silencio e a reconexao
    /// nunca dispara, porque nada avisa que caiu.
    ///
    /// C# puro, cronometro injetado pelo chamador em <see cref="Tick"/>: a logica de
    /// tempo roda em teste sem esperar segundo nenhum.
    /// </summary>
    /// <example>
    /// <code>
    /// if (heartbeat.Tick(delta.TotalSeconds) == HeartbeatAction.SendPing) SendPing();
    /// </code>
    /// </example>
    public sealed class Heartbeat
    {
        private readonly double intervalSeconds;
        private readonly double timeoutSeconds;

        private double sinceLastPing;
        private double sinceLastInbound;

        /// <summary>
        /// Liga o timeout so depois do primeiro pong.
        ///
        /// Contra um servidor que ainda nao responde ping, o cronometro nunca
        /// dispara e o comportamento e o de nao ter heartbeat -- em vez de derrubar
        /// uma conexao boa a cada timeout.
        /// </summary>
        private bool serverAnswersPing;

        /// <param name="intervalSeconds">Espaco entre pings.</param>
        /// <param name="timeoutSeconds">
        /// Silencio total aceito antes de declarar morta. Precisa ser bem maior que
        /// o intervalo: com folga para varios pings, um pico de latencia sozinho
        /// nao derruba a conexao.
        /// </param>
        /// <summary>Heartbeat com intervalo de ping e silencio aceito, em segundos.</summary>
        /// <example><code>Heartbeat heartbeat = new Heartbeat(intervalSeconds: 10.0, timeoutSeconds: 30.0);</code></example>
        public Heartbeat(double intervalSeconds = 10.0, double timeoutSeconds = 30.0)
        {
            if (intervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), intervalSeconds, "precisa ser maior que zero");

            if (timeoutSeconds <= intervalSeconds)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, $"precisa ser maior que intervalSeconds ({intervalSeconds})");

            this.intervalSeconds = intervalSeconds;
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <summary>Silencio acumulado desde a ultima mensagem recebida.</summary>
        /// <example><code>double silence = heartbeat.SilenceSeconds;</code></example>
        public double SilenceSeconds => sinceLastInbound;

        /// <summary>Zera tudo. Chamar a cada socket novo.</summary>
        /// <example><code>heartbeat.Reset();</code></example>
        public void Reset()
        {
            sinceLastPing = 0;
            sinceLastInbound = 0;
            serverAnswersPing = false;
        }

        /// <summary>
        /// Chegou mensagem. Qualquer uma serve de prova de vida, nao so o pong.
        /// </summary>
        /// <example><code>heartbeat.NoteInbound();</code></example>
        public void NoteInbound()
        {
            sinceLastInbound = 0;
        }

        /// <summary>Chegou pong: o servidor fala o protocolo, o timeout passa a valer.</summary>
        /// <example><code>heartbeat.NotePong();</code></example>
        public void NotePong()
        {
            serverAnswersPing = true;
            sinceLastInbound = 0;
        }

        /// <summary>
        /// Tempo pausado nao e silencio do servidor: zera o silencio sem desarmar o timeout. Chamado na
        /// volta ao primeiro plano e quando dois quadros ficam longe demais
        /// (specs/003-authenticated-socket-queue/research.md, R3).
        /// </summary>
        /// <example><code>heartbeat.ForgivePause();</code></example>
        public void ForgivePause()
        {
            sinceLastInbound = 0;
        }

        /// <summary>Saiu um ping fora do ciclo (abertura, volta): o proximo espera o intervalo inteiro.</summary>
        /// <example><code>heartbeat.NotePingSent();</code></example>
        public void NotePingSent()
        {
            sinceLastPing = 0;
        }

        /// <summary>Avanca o relogio e devolve a acao deste frame.</summary>
        /// <example><code>HeartbeatAction action = heartbeat.Tick(0.016);</code></example>
        public HeartbeatAction Tick(double deltaSeconds)
        {
            sinceLastPing += deltaSeconds;
            sinceLastInbound += deltaSeconds;

            // Morte primeiro: nao adianta mandar ping por um cano que ja caiu.
            if (serverAnswersPing && sinceLastInbound >= timeoutSeconds)
                return HeartbeatAction.DeclareDead;

            if (sinceLastPing < intervalSeconds)
                return HeartbeatAction.Idle;

            sinceLastPing = 0;

            return HeartbeatAction.SendPing;
        }
    }
}
