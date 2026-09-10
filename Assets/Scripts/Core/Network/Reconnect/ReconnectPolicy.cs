using System;

/// <summary>O que o cliente deve fazer depois que o socket fechou.</summary>
public enum ReconnectAction
{
    /// <summary>Espera o delay e conecta de novo com o token atual.</summary>
    Retry,

    /// <summary>Renova o access token antes de tentar: o servidor recusou este.</summary>
    RefreshTokenThenRetry,

    /// <summary>Reconectar nao vai adiantar. Avisa o jogador.</summary>
    GiveUp,
}

/// <summary>A decisao, com o motivo junto para virar log e mensagem de UI.</summary>
public readonly struct ReconnectPlan
{
    public readonly ReconnectAction Action;
    public readonly double DelaySeconds;
    public readonly string Reason;

    public ReconnectPlan(ReconnectAction action, double delaySeconds, string reason)
    {
        Action = action;
        DelaySeconds = delaySeconds;
        Reason = reason;
    }

    public override string ToString()
    {
        return $"{Action} em {DelaySeconds:0.##}s ({Reason})";
    }
}

/// <summary>
/// Decide se e quando reconectar, a partir do codigo de close.
///
/// C# puro de proposito: nao depende do Unity nem do socket, entao a parte
/// que mais quebra em silencio (o backoff e a lista de codigos terminais) roda
/// em teste sem abrir cena nem servidor.
///
/// Um objeto por client: cada um tem sua contagem de tentativas e sua politica.
/// </summary>
public sealed class ReconnectPolicy
{
    // Codigos do backend, em apps/game/consumers/. 4001 e o gate de auth do
    // BaseConsumer; a serie 44xx e o gate de partida do MatchConsumer.
    public const int AuthRejected = 4001;
    public const int MatchIdMissing = 4400;
    public const int NotAParticipant = 4403;
    public const int MatchNotFound = 4404;

    // Do proprio protocolo WebSocket.
    public const int NormalClosure = 1000;
    public const int GoingAway = 1001;
    public const int AbnormalClosure = 1006;

    private readonly double baseDelaySeconds;
    private readonly double maxDelaySeconds;
    private readonly int maxAttempts;
    private readonly int maxAuthRetries;
    private readonly double jitterRatio;
    private readonly Func<double> random;

    private int authFailures;

    /// <summary>Quantas vezes ja tentamos desde o ultimo <see cref="Reset"/>.</summary>
    public int Attempt { get; private set; }

    /// <param name="baseDelaySeconds">Espera da primeira tentativa. Dobra a cada falha.</param>
    /// <param name="maxDelaySeconds">Teto da espera.</param>
    /// <param name="maxAttempts">Tentativas antes de desistir.</param>
    /// <param name="maxAuthRetries">
    /// Quantos 4001 seguidos toleramos. Passou disso, renovar o token nao esta
    /// resolvendo e insistir so gera carga no servidor.
    /// </param>
    /// <param name="jitterRatio">
    /// Variacao aleatoria aplicada ao delay, de 0 a 1. Importa quando o
    /// servidor reinicia: sem jitter, todos os clientes voltam no mesmo
    /// milissegundo e derrubam ele de novo.
    /// </param>
    /// <param name="random">
    /// Fonte de aleatoriedade em [0,1). Injetavel para o teste ser deterministico.
    /// </param>
    public ReconnectPolicy(
        double baseDelaySeconds = 0.5,
        double maxDelaySeconds = 30.0,
        int maxAttempts = int.MaxValue,
        int maxAuthRetries = 2,
        double jitterRatio = 0.2,
        Func<double> random = null)
    {
        if (baseDelaySeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseDelaySeconds), "precisa ser maior que zero");

        if (maxDelaySeconds < baseDelaySeconds)
            throw new ArgumentOutOfRangeException(nameof(maxDelaySeconds), "nao pode ser menor que baseDelaySeconds");

        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "precisa ser pelo menos 1");

        if (maxAuthRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAuthRetries), "nao pode ser negativo");

        if (jitterRatio < 0 || jitterRatio > 1)
            throw new ArgumentOutOfRangeException(nameof(jitterRatio), "precisa estar entre 0 e 1");

        this.baseDelaySeconds = baseDelaySeconds;
        this.maxDelaySeconds = maxDelaySeconds;
        this.maxAttempts = maxAttempts;
        this.maxAuthRetries = maxAuthRetries;
        this.jitterRatio = jitterRatio;
        this.random = random ?? NewThreadSafeRandom();
    }

    /// <summary>
    /// Zera tudo: backoff e historico de recusa de token. Chamar quando a
    /// conexao provar que funciona, nao so quando o socket abrir.
    /// </summary>
    public void Reset()
    {
        Attempt = 0;
        authFailures = 0;
    }

    /// <summary>
    /// Zera so o backoff, preservando quantas vezes o token ja foi recusado.
    ///
    /// Existe porque o gate de autenticacao aceita o socket antes de fechar,
    /// para conseguir mandar o motivo junto. O socket abre mesmo quando o
    /// token e invalido, e limpar o historico de auth nesse ponto faria o
    /// limite de <c>maxAuthRetries</c> nunca ser alcancado.
    /// </summary>
    public void ResetBackoff()
    {
        Attempt = 0;
    }

    /// <summary>Decide o que fazer com o socket que acabou de fechar.</summary>
    public ReconnectPlan OnClosed(int closeCode)
    {
        if (IsTerminal(closeCode, out var reason))
            return new ReconnectPlan(ReconnectAction.GiveUp, 0, reason);

        Attempt++;

        if (Attempt > maxAttempts)
        {
            return new ReconnectPlan(
                ReconnectAction.GiveUp,
                0,
                $"desisti apos {maxAttempts} tentativa(s)");
        }

        var delay = NextDelay();

        if (closeCode != AuthRejected)
            return new ReconnectPlan(ReconnectAction.Retry, delay, $"close {closeCode}");

        authFailures++;

        if (authFailures > maxAuthRetries)
        {
            return new ReconnectPlan(
                ReconnectAction.GiveUp,
                0,
                $"token recusado {authFailures} vezes seguidas");
        }

        return new ReconnectPlan(
            ReconnectAction.RefreshTokenThenRetry,
            delay,
            $"close {AuthRejected}: token recusado");
    }

    private static bool IsTerminal(int closeCode, out string reason)
    {
        switch (closeCode)
        {
            case NormalClosure:
                // Fechamento limpo vindo do servidor quer dizer "acabou".
                // Deploy manda 1001, que continua sendo reconectavel.
                reason = "servidor encerrou a conexao normalmente";
                return true;

            case MatchIdMissing:
                reason = "cliente conectou sem matchId";
                return true;

            case NotAParticipant:
                reason = "jogador nao participa desta partida";
                return true;

            case MatchNotFound:
                reason = "partida nao existe mais";
                return true;

            default:
                reason = null;
                return false;
        }
    }

    private double NextDelay()
    {
        // Math.Pow satura em infinito com Attempt alto, e o Min corta antes de
        // isso virar um delay invalido.
        var growth = baseDelaySeconds * Math.Pow(2, Attempt - 1);
        var capped = Math.Min(growth, maxDelaySeconds);

        if (jitterRatio <= 0)
            return capped;

        // random() em [0,1) vira um fator em [1 - ratio, 1 + ratio).
        var factor = 1 + jitterRatio * (2 * random() - 1);

        return capped * factor;
    }

    private static Func<double> NewThreadSafeRandom()
    {
        var source = new Random();

        // O socket fecha em thread de rede, e Random nao e thread-safe:
        // duas chamadas concorrentes deixam a instancia devolvendo 0 para sempre.
        return () =>
        {
            lock (source)
                return source.NextDouble();
        };
    }
}
