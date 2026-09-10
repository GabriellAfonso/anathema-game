using System;

/// <summary>O que o client deve fazer neste frame.</summary>
public enum HeartbeatAction
{
    /// <summary>Nada a fazer.</summary>
    Idle,

    /// <summary>Hora de mandar um ping.</summary>
    SendPing,

    /// <summary>Silencio longo demais: trate a conexao como morta.</summary>
    DeclareDead,
}

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
    public Heartbeat(double intervalSeconds = 10.0, double timeoutSeconds = 30.0)
    {
        if (intervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "precisa ser maior que zero");

        if (timeoutSeconds <= intervalSeconds)
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "precisa ser maior que intervalSeconds");

        this.intervalSeconds = intervalSeconds;
        this.timeoutSeconds = timeoutSeconds;
    }

    /// <summary>Silencio acumulado desde a ultima mensagem recebida.</summary>
    public double SilenceSeconds => sinceLastInbound;

    /// <summary>Zera tudo. Chamar a cada socket novo.</summary>
    public void Reset()
    {
        sinceLastPing = 0;
        sinceLastInbound = 0;
        serverAnswersPing = false;
    }

    /// <summary>
    /// Chegou mensagem. Qualquer uma serve de prova de vida, nao so o pong.
    /// </summary>
    public void NoteInbound()
    {
        sinceLastInbound = 0;
    }

    /// <summary>Chegou pong: o servidor fala o protocolo, o timeout passa a valer.</summary>
    public void NotePong()
    {
        serverAnswersPing = true;
        sinceLastInbound = 0;
    }

    /// <summary>Avanca o relogio e devolve a acao deste frame.</summary>
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
