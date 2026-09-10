using System;
using NUnit.Framework;

public class ReconnectPolicyTests
{
    /// <summary>Sem jitter e com delays exatos, o backoff vira aritmetica verificavel.</summary>
    private static ReconnectPolicy Deterministic(
        double baseDelay = 1.0,
        double maxDelay = 30.0,
        int maxAttempts = int.MaxValue,
        int maxAuthRetries = 2)
    {
        return new ReconnectPolicy(
            baseDelaySeconds: baseDelay,
            maxDelaySeconds: maxDelay,
            maxAttempts: maxAttempts,
            maxAuthRetries: maxAuthRetries,
            jitterRatio: 0,
            random: () => 0.5);
    }

    // ---------- backoff ----------

    [Test]
    public void PrimeiraTentativaUsaODelayBase()
    {
        var plan = Deterministic(baseDelay: 0.5).OnClosed(ReconnectPolicy.AbnormalClosure);

        Assert.AreEqual(ReconnectAction.Retry, plan.Action);
        Assert.AreEqual(0.5, plan.DelaySeconds, 1e-9);
    }

    [Test]
    public void DelayDobraACadaFalha()
    {
        var policy = Deterministic(baseDelay: 1.0);

        Assert.AreEqual(1.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
        Assert.AreEqual(2.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
        Assert.AreEqual(4.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
        Assert.AreEqual(8.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
    }

    [Test]
    public void DelayParaDeCrescerNoTeto()
    {
        var policy = Deterministic(baseDelay: 1.0, maxDelay: 5.0);

        for (var i = 0; i < 20; i++)
            policy.OnClosed(ReconnectPolicy.AbnormalClosure);

        Assert.AreEqual(5.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
    }

    [Test]
    public void DelayNuncaViraInfinitoComMuitasTentativas()
    {
        var policy = Deterministic(baseDelay: 1.0, maxDelay: 30.0);

        for (var i = 0; i < 5000; i++)
        {
            var plan = policy.OnClosed(ReconnectPolicy.AbnormalClosure);
            Assert.IsFalse(double.IsInfinity(plan.DelaySeconds), "delay virou infinito");
            Assert.IsFalse(double.IsNaN(plan.DelaySeconds), "delay virou NaN");
        }
    }

    [Test]
    public void ResetVoltaParaODelayBase()
    {
        var policy = Deterministic(baseDelay: 1.0);

        policy.OnClosed(ReconnectPolicy.AbnormalClosure);
        policy.OnClosed(ReconnectPolicy.AbnormalClosure);
        Assert.AreEqual(2, policy.Attempt);

        policy.Reset();

        Assert.AreEqual(0, policy.Attempt);
        Assert.AreEqual(1.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
    }

    [Test]
    public void ResetBackoffLimpaODelayMasGuardaOHistoricoDeAuth()
    {
        // O gate de autenticacao aceita o socket antes de fechar, entao o
        // OnOpen dispara mesmo com token invalido. Se isso limpasse o
        // historico de auth, maxAuthRetries nunca seria alcancado.
        var policy = Deterministic(maxAuthRetries: 1);

        Assert.AreEqual(
            ReconnectAction.RefreshTokenThenRetry,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);

        policy.ResetBackoff();

        Assert.AreEqual(0, policy.Attempt, "o backoff devia ter voltado ao inicio");
        Assert.AreEqual(
            ReconnectAction.GiveUp,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action,
            "a segunda recusa de token devia desistir, nao tentar de novo");
    }

    // ---------- jitter ----------

    [Test]
    public void JitterFicaDentroDaFaixa()
    {
        // random() nos extremos: 0 vira o piso, 1 (exclusivo) vira o teto.
        var piso = new ReconnectPolicy(
            baseDelaySeconds: 10, jitterRatio: 0.2, random: () => 0.0);
        var teto = new ReconnectPolicy(
            baseDelaySeconds: 10, jitterRatio: 0.2, random: () => 1.0);

        Assert.AreEqual(8.0, piso.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
        Assert.AreEqual(12.0, teto.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
    }

    [Test]
    public void JitterZeroDevolveODelayExato()
    {
        var policy = new ReconnectPolicy(
            baseDelaySeconds: 10, jitterRatio: 0, random: () => 0.0);

        Assert.AreEqual(10.0, policy.OnClosed(ReconnectPolicy.AbnormalClosure).DelaySeconds, 1e-9);
    }

    // ---------- codigos terminais ----------

    [Test]
    public void CodigosTerminaisDesistemSemGastarTentativa()
    {
        int[] terminais =
        {
            ReconnectPolicy.NormalClosure,
            ReconnectPolicy.MatchIdMissing,
            ReconnectPolicy.NotAParticipant,
            ReconnectPolicy.MatchNotFound,
        };

        foreach (var code in terminais)
        {
            var policy = Deterministic();
            var plan = policy.OnClosed(code);

            Assert.AreEqual(ReconnectAction.GiveUp, plan.Action, $"close {code} devia desistir");
            Assert.AreEqual(0, plan.DelaySeconds, $"close {code} nao devia agendar delay");
            Assert.AreEqual(0, policy.Attempt, $"close {code} nao devia contar tentativa");
            Assert.IsNotNull(plan.Reason, $"close {code} devia explicar o motivo");
        }
    }

    [Test]
    public void ServidorSaindoDoArEhReconectavel()
    {
        // 1001 e o que um deploy manda. Diferente de 1000, vale voltar.
        var plan = Deterministic().OnClosed(ReconnectPolicy.GoingAway);

        Assert.AreEqual(ReconnectAction.Retry, plan.Action);
    }

    [Test]
    public void CodigoDesconhecidoTentaDeNovo()
    {
        var plan = Deterministic().OnClosed(4999);

        Assert.AreEqual(ReconnectAction.Retry, plan.Action);
    }

    // ---------- auth ----------

    [Test]
    public void TokenRecusadoPedeRefreshAntesDeTentar()
    {
        var plan = Deterministic().OnClosed(ReconnectPolicy.AuthRejected);

        Assert.AreEqual(ReconnectAction.RefreshTokenThenRetry, plan.Action);
        Assert.Greater(plan.DelaySeconds, 0);
    }

    [Test]
    public void TokenRecusadoDemaisVezesDesiste()
    {
        var policy = Deterministic(maxAuthRetries: 2);

        Assert.AreEqual(
            ReconnectAction.RefreshTokenThenRetry,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);
        Assert.AreEqual(
            ReconnectAction.RefreshTokenThenRetry,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);

        // Terceiro 4001 seguido: renovar o token nao esta resolvendo.
        Assert.AreEqual(
            ReconnectAction.GiveUp,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);
    }

    [Test]
    public void ConexaoBemSucedidaLimpaAContagemDeAuth()
    {
        var policy = Deterministic(maxAuthRetries: 1);

        Assert.AreEqual(
            ReconnectAction.RefreshTokenThenRetry,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);

        policy.Reset();

        Assert.AreEqual(
            ReconnectAction.RefreshTokenThenRetry,
            policy.OnClosed(ReconnectPolicy.AuthRejected).Action);
    }

    // ---------- limite de tentativas ----------

    [Test]
    public void DesisteDepoisDoMaximoDeTentativas()
    {
        var policy = Deterministic(maxAttempts: 3);

        Assert.AreEqual(ReconnectAction.Retry, policy.OnClosed(ReconnectPolicy.AbnormalClosure).Action);
        Assert.AreEqual(ReconnectAction.Retry, policy.OnClosed(ReconnectPolicy.AbnormalClosure).Action);
        Assert.AreEqual(ReconnectAction.Retry, policy.OnClosed(ReconnectPolicy.AbnormalClosure).Action);
        Assert.AreEqual(ReconnectAction.GiveUp, policy.OnClosed(ReconnectPolicy.AbnormalClosure).Action);
    }

    // ---------- validacao de argumentos ----------

    [Test]
    public void ArgumentosInvalidosSaoRejeitadosNaConstrucao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReconnectPolicy(baseDelaySeconds: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReconnectPolicy(baseDelaySeconds: 10, maxDelaySeconds: 5));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReconnectPolicy(maxAttempts: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReconnectPolicy(maxAuthRetries: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReconnectPolicy(jitterRatio: 1.5));
    }
}
