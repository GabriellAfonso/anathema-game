#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Decide se e quando reconectar, a partir do codigo de close.
    ///
    /// C# puro de proposito: nao depende do Unity nem do socket, entao a parte
    /// que mais quebra em silencio (o backoff e a lista de codigos terminais) roda
    /// em teste sem abrir cena nem servidor.
    ///
    /// Um objeto por client: cada um tem sua contagem de tentativas e sua politica.
    /// </summary>
    /// <example>
    /// <code>
    /// ReconnectPlan plan = policy.OnClosed(closure.Code ?? ReconnectPolicy.AbnormalClosure);
    /// </code>
    /// </example>
    public sealed class ReconnectPolicy
    {
        // Codigos do backend, em apps/game/consumers/. 4001 e o gate de auth do
        // BaseConsumer; a serie 44xx e o gate de partida do MatchConsumer.

        /// <summary>Gate de autenticacao recusou o access token.</summary>
        /// <example><code>ReconnectPlan plan = policy.OnClosed(ReconnectPolicy.AuthRejected);</code></example>
        public const int AuthRejected = 4001;

        /// <summary>Gate de partida: o socket abriu sem matchId.</summary>
        /// <example><code>bool terminal = code == ReconnectPolicy.MatchIdMissing;</code></example>
        public const int MatchIdMissing = 4400;

        /// <summary>Gate de partida: o jogador nao joga esta partida.</summary>
        /// <example><code>bool terminal = code == ReconnectPolicy.NotAParticipant;</code></example>
        public const int NotAParticipant = 4403;

        /// <summary>Gate de partida: a partida nao existe mais.</summary>
        /// <example><code>bool terminal = code == ReconnectPolicy.MatchNotFound;</code></example>
        public const int MatchNotFound = 4404;

        // Do proprio protocolo WebSocket.

        /// <summary>Fechamento limpo.</summary>
        /// <example><code>ReconnectPlan plan = policy.OnClosed(ReconnectPolicy.NormalClosure);</code></example>
        public const int NormalClosure = 1000;

        /// <summary>O servidor esta saindo do ar (deploy).</summary>
        /// <example><code>ReconnectPlan plan = policy.OnClosed(ReconnectPolicy.GoingAway);</code></example>
        public const int GoingAway = 1001;

        /// <summary>Queda sem close frame.</summary>
        /// <example><code>ReconnectPlan plan = policy.OnClosed(ReconnectPolicy.AbnormalClosure);</code></example>
        public const int AbnormalClosure = 1006;

        private readonly double baseDelaySeconds;
        private readonly double maxDelaySeconds;
        private readonly int maxAttempts;
        private readonly int maxAuthRetries;
        private readonly double jitterRatio;
        private readonly Func<double> random;

        private int authFailures;

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
        /// <summary>Politica com backoff exponencial, teto, jitter e limites de tentativa e de recusa de token.</summary>
        /// <example><code>ReconnectPolicy policy = new ReconnectPolicy(baseDelaySeconds: 0.5, maxDelaySeconds: 5.0, maxAttempts: 5);</code></example>
        public ReconnectPolicy(
            double baseDelaySeconds = 0.5,
            double maxDelaySeconds = 30.0,
            int maxAttempts = int.MaxValue,
            int maxAuthRetries = 2,
            double jitterRatio = 0.2,
            Func<double>? random = null)
        {
            RequireDelays(baseDelaySeconds, maxDelaySeconds);
            RequireLimits(maxAttempts, maxAuthRetries, jitterRatio);

            this.baseDelaySeconds = baseDelaySeconds;
            this.maxDelaySeconds = maxDelaySeconds;
            this.maxAttempts = maxAttempts;
            this.maxAuthRetries = maxAuthRetries;
            this.jitterRatio = jitterRatio;
            this.random = random ?? NewThreadSafeRandom();
        }

        /// <summary>Quantas vezes ja tentamos desde o ultimo <see cref="Reset"/>.</summary>
        /// <example><code>int attempt = policy.Attempt;</code></example>
        public int Attempt { get; private set; }

        /// <summary>Espera da primeira tentativa, em segundos.</summary>
        /// <example><code>double first = policy.BaseDelaySeconds;</code></example>
        public double BaseDelaySeconds => baseDelaySeconds;

        /// <summary>Teto da espera, em segundos.</summary>
        /// <example><code>double cap = policy.MaxDelaySeconds;</code></example>
        public double MaxDelaySeconds => maxDelaySeconds;

        /// <summary>Tentativas seguidas, sem prova de sessao, antes de desistir.</summary>
        /// <example><code>int limit = policy.MaxAttempts;</code></example>
        public int MaxAttempts => maxAttempts;

        /// <summary>Recusas de token seguidas toleradas.</summary>
        /// <example><code>int refusals = policy.MaxAuthRetries;</code></example>
        public int MaxAuthRetries => maxAuthRetries;

        /// <summary>Variacao aleatoria aplicada ao delay, de 0 a 1.</summary>
        /// <example><code>double jitter = policy.JitterRatio;</code></example>
        public double JitterRatio => jitterRatio;

        /// <summary>
        /// Zera tudo: backoff e historico de recusa de token. Chamar quando a
        /// conexao provar que funciona, nao so quando o socket abrir.
        /// </summary>
        /// <example><code>policy.Reset();</code></example>
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
        /// <example><code>policy.ResetBackoff();</code></example>
        public void ResetBackoff()
        {
            Attempt = 0;
        }

        /// <summary>Decide o que fazer com o socket que acabou de fechar.</summary>
        /// <example><code>ReconnectPlan plan = policy.OnClosed(4001);</code></example>
        public ReconnectPlan OnClosed(int closeCode)
        {
            string? terminalReason = TerminalReason(closeCode);
            if (terminalReason != null)
                return new ReconnectPlan(ReconnectAction.GiveUp, 0, terminalReason);

            Attempt++;

            if (Attempt > maxAttempts)
                return new ReconnectPlan(ReconnectAction.GiveUp, 0, $"desisti apos {maxAttempts} tentativa(s)");

            double delay = NextDelay();

            if (closeCode != AuthRejected)
                return new ReconnectPlan(ReconnectAction.Retry, delay, $"close {closeCode}");

            return PlanAfterTokenRefusal(delay);
        }

        private ReconnectPlan PlanAfterTokenRefusal(double delay)
        {
            authFailures++;

            if (authFailures > maxAuthRetries)
                return new ReconnectPlan(ReconnectAction.GiveUp, 0, $"token recusado {authFailures} vezes seguidas");

            return new ReconnectPlan(ReconnectAction.RefreshTokenThenRetry, delay, $"close {AuthRejected}: token recusado");
        }

        private static string? TerminalReason(int closeCode)
        {
            return closeCode switch
            {
                // Fechamento limpo vindo do servidor queria dizer "acabou". Deixou de ser terminal: o
                // servidor nao fecha socket ocioso nem o de partida no fim, entao um 1000 dele so vem
                // de reinicio ou deploy, como o 1001, e vale voltar
                // (specs/003-authenticated-socket-queue/spec.md, Assumptions).
                MatchIdMissing => "cliente conectou sem matchId",
                NotAParticipant => "jogador nao participa desta partida",
                MatchNotFound => "partida nao existe mais",
                _ => null,
            };
        }

        private double NextDelay()
        {
            // Math.Pow satura em infinito com Attempt alto, e o Min corta antes de
            // isso virar um delay invalido.
            double growth = baseDelaySeconds * Math.Pow(2, Attempt - 1);
            double capped = Math.Min(growth, maxDelaySeconds);

            if (jitterRatio <= 0)
                return capped;

            // random() em [0,1) vira um fator em [1 - ratio, 1 + ratio).
            double factor = 1 + jitterRatio * (2 * random() - 1);

            return capped * factor;
        }

        private static void RequireDelays(double baseDelaySeconds, double maxDelaySeconds)
        {
            if (baseDelaySeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseDelaySeconds), baseDelaySeconds, "precisa ser maior que zero");

            if (maxDelaySeconds < baseDelaySeconds)
                throw new ArgumentOutOfRangeException(nameof(maxDelaySeconds), maxDelaySeconds, $"nao pode ser menor que baseDelaySeconds ({baseDelaySeconds})");
        }

        private static void RequireLimits(int maxAttempts, int maxAuthRetries, double jitterRatio)
        {
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "precisa ser pelo menos 1");

            if (maxAuthRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAuthRetries), maxAuthRetries, "nao pode ser negativo");

            if (jitterRatio < 0 || jitterRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(jitterRatio), jitterRatio, "precisa estar entre 0 e 1");
        }

        private static Func<double> NewThreadSafeRandom()
        {
            Random source = new Random();

            // O socket fecha em thread de rede, e Random nao e thread-safe:
            // duas chamadas concorrentes deixam a instancia devolvendo 0 para sempre.
            return () =>
            {
                lock (source)
                    return source.NextDouble();
            };
        }
    }
}
