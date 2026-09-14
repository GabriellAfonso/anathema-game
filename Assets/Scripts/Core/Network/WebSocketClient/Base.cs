#nullable enable
using System;
using Anathema.Net.Connection;

/// <summary>
/// Adaptador do codigo de cena sobre a <see cref="AuthenticatedConnection"/> (feature 003). Mantem os
/// eventos que o <see cref="ReconnectOverlay"/> ja assina. Token, reconexao, heartbeat, segundo plano,
/// troca de rede e despacho moram na conexao, testados em Anathema.Net.Connection.Tests. Sai quando a
/// feature 5 der a fachada a apresentacao.
/// </summary>
/// <example><code>ReconnectOverlay.Attach(PlayerSession.Instance.Matchmaking, PlayerSession.Instance.Match);</code></example>
public abstract class BaseClient
{
    // Numero da ultima tentativa anunciada: a suspensao sem rede nao carrega tentativa propria, e o
    // aviso continua mostrando a da queda que a causou.
    private int lastAttempt = 1;

    /// <summary>Cliente sobre a conexao composta pela raiz das cenas.</summary>
    /// <example><code>protected MatchClient(AuthenticatedConnection connection) : base(connection) { }</code></example>
    protected BaseClient(AuthenticatedConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection), "connection is null: expected the AuthenticatedConnection composed by PlayerSession");
        Connection.StatusChanged += TranslateStatus;
        Connection.Recovered += () => OnReconnected?.Invoke();
    }

    /// <summary>Caiu e vai voltar: numero da tentativa e quantos segundos faltam.</summary>
    public event Action<int, double>? OnReconnecting;

    /// <summary>Voltou depois de ter caido. Hora de pedir o estado ao servidor.</summary>
    public event Action? OnReconnected;

    /// <summary>Nao vai mais tentar. O motivo serve de mensagem para o jogador.</summary>
    public event Action<string>? OnGaveUp;

    /// <summary>A conexao por baixo deste cliente.</summary>
    /// <example><code>ConnectionPhase phase = Connection.Status.Phase;</code></example>
    protected AuthenticatedConnection Connection { get; }

    /// <summary>Sai de proposito. Nao reconecta.</summary>
    /// <example><code>PlayerSession.Instance.Match.Disconnect();</code></example>
    public void Disconnect()
    {
        Connection.Leave();
    }

    private void TranslateStatus(ConnectionStatus status)
    {
        if (status.Phase == ConnectionPhase.WaitingRetry)
            AnnounceRetry(status.Attempt, status.Wait.TotalSeconds);
        else if (status.Phase == ConnectionPhase.Suspended && status.Suspension == SuspensionReason.NoNetwork)
            AnnounceRetry(lastAttempt, 0);
        else if (status.Phase == ConnectionPhase.GaveUp)
            OnGaveUp?.Invoke(status.GiveUp!.PlayerText());
    }

    private void AnnounceRetry(int attempt, double waitSeconds)
    {
        lastAttempt = attempt;
        OnReconnecting?.Invoke(attempt, waitSeconds);
    }
}
