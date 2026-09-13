#nullable enable
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Anathema.Config.Tests
{
    public class AppConfigTests
    {
        private AppConfig config = null!;

        [SetUp]
        public void CreateConfig()
        {
            config = ScriptableObject.CreateInstance<AppConfig>();
            config.apiBaseUrl = "127.0.0.1:8000";
            config.matchmakingConsumerUrl = "/ws/matchmaking/";
        }

        [TearDown]
        public void DestroyConfig()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SemOverrideUsaOHostDoAsset()
        {
            Assert.That(config.EffectiveHost, Is.EqualTo("127.0.0.1:8000"));
            Assert.That(config.HttpUrl("/accounts/login/"), Is.EqualTo("http://127.0.0.1:8000/accounts/login/"));
        }

        [Test]
        public void EsquemaSegueUseTls()
        {
            config.useTls = true;

            Assert.That(config.WsUrl(config.matchmakingConsumerUrl), Is.EqualTo("wss://127.0.0.1:8000/ws/matchmaking/"));
        }

        [Test]
        public void OverrideEmDesenvolvimentoTrocaAsUrls()
        {
            string? problem = config.OverrideHost("http://192.168.0.10:8000/", developmentBuild: true);

            Assert.That(problem, Is.Null);
            Assert.That(config.WsUrl(config.matchmakingConsumerUrl), Is.EqualTo("ws://192.168.0.10:8000/ws/matchmaking/"));
        }

        [Test]
        public void HostInvalidoDevolveOMotivoEMantemOAnterior()
        {
            config.OverrideHost("192.168.0.10:8000", developmentBuild: true);

            string? problem = config.OverrideHost("192.168.0.10:99999", developmentBuild: true);

            Assert.That(problem, Does.Contain("192.168.0.10:99999"));
            Assert.That(config.EffectiveHost, Is.EqualTo("192.168.0.10:8000"));
        }

        [Test]
        public void ProducaoRecusaOverride()
        {
            string? problem = config.OverrideHost("192.168.0.10:8000", developmentBuild: false);

            Assert.That(problem, Does.Contain("development"));
            Assert.That(config.EffectiveHost, Is.EqualTo("127.0.0.1:8000"));
        }

        [Test]
        public void OverrideVazioDeixadoPelaRecargaDeScriptsNaoApagaOHost()
        {
            // Regressão: ao entrar em Play, o Unity serializa os campos privados de objetos em
            // memória e não representa string nula; overrideHost voltava "" e a URL saía http:///.
            typeof(AppConfig).GetField("overrideHost", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(config, "");

            Assert.That(config.EffectiveHost, Is.EqualTo("127.0.0.1:8000"));
        }

        [TestCase("overrideHost")]
        [TestCase("launchHostApplied")]
        public void EstadoDeExecucaoNaoEntraNaSerializacao(string field)
        {
            FieldInfo? runtimeField = typeof(AppConfig).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(runtimeField, Is.Not.Null);
            Assert.That(runtimeField!.IsNotSerialized, Is.True, $"{field} must be [NonSerialized]: domain reload would restore it");
        }

        [Test]
        public void RotaDoSocketDePresencaSaiu()
        {
            Assert.That(typeof(AppConfig).GetField("connectionConsumerUrl", BindingFlags.Public | BindingFlags.Instance), Is.Null);
        }
    }
}
