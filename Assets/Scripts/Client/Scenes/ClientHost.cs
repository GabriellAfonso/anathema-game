#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Unity;
using UnityEngine;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// O único hospedeiro da camada nas cenas: escolhe a configuração de desenvolvimento ou de produção, compõe
    /// adaptadores e fachada, liga o passo de cada quadro, entrega a fachada às cenas pelo binder e segue o estado do
    /// app pelo roteador. Veio do antigo <c>PlayerSession</c> (mesmo <c>.meta</c>, para a <c>BootstrapScene</c>
    /// continuar referenciando) e absorveu o <c>AppEnvManager</c>; não há mais singleton de composição
    /// (specs/005-presentation-facade/research.md, R9).
    /// </summary>
    /// <example><code>// Componente do objeto ClientHost da BootstrapScene, com configDev e configProd no Inspector.</code></example>
    public sealed class ClientHost : MonoBehaviour
    {
        // Preenchidos pelo Inspector na BootstrapScene; o valor inicial só cala o aviso de campo nunca atribuído.
        [SerializeField] private AppConfig? configDev = null;
        [SerializeField] private AppConfig? configProd = null;
        [SerializeField] private bool isProd = false;

        private ComposedClient? composed;
        private SceneRouter? router;
        private SceneClientBinder? binder;

        private void Awake()
        {
            IClientLog log = ClientComposition.CreateConsoleLog();
            AppConfig? settings = SelectConfig(log);
            if (settings == null)
                return;

            DontDestroyOnLoad(gameObject);
            composed = ClientComposition.Compose(new ClientCompositionOptions(settings.BuildServerRoutes(), Debug.isDebugBuild) { Log = log });
            // O UnityHttpTransport completa as tarefas pela fila; sem o hospedeiro drenando, nenhum login termina.
            composed.AttachTo(gameObject);
        }

        private void Start()
        {
            if (composed == null)
                return;

            // Aqui, e não num RuntimeInitializeOnLoadMethod: é o único ponto em que a fachada já existe e as cenas
            // abertas junto da de bootstrap já acordaram.
            binder = new SceneClientBinder(composed.Client);
            router = new SceneRouter(composed.Client);
            binder.BindLoadedScenes();
            binder.BindScene(gameObject.scene);
            router.Route(composed.Client.State.Stage);
        }

        private AppConfig? SelectConfig(IClientLog log)
        {
            AppConfig? settings = isProd ? configProd : configDev;
            if (settings == null)
            {
                log.Error("app_config_missing", new LogField("field", isProd ? "configProd" : "configDev"), new LogField("expected", "an AppConfig asset assigned in the Inspector"));
                return null;
            }

            WarnAboutConfig(settings, log);
            log.Info("app_config_loaded", new LogField("environment", isProd ? "production" : "development"), new LogField("asset", settings.name));
            return settings;
        }

        private void WarnAboutConfig(AppConfig settings, IClientLog log)
        {
            if (string.IsNullOrEmpty(settings.apiBaseUrl))
                log.Error("app_config_host_empty", new LogField("asset", settings.name));

            // Produção sem TLS: o token JWT trafegaria em texto puro.
            if (isProd && !settings.useTls)
                log.Error("app_config_production_without_tls", new LogField("asset", settings.name));
        }

        private void OnDestroy()
        {
            binder?.Dispose();
            router?.Dispose();
            composed?.Dispose();
        }
    }
}
