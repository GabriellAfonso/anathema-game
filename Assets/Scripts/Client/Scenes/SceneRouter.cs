#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// Segue o estado do app e carrega a cena de cada estágio. Só carrega se a cena de destino não estiver carregada:
    /// o editor abre a de login junto da de bootstrap, e a queda e volta da partida não mudam o estágio, então a cena
    /// da partida nunca é recarregada por reconexão (specs/005-presentation-facade/research.md, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// SceneRouter router = new SceneRouter(client);
    /// router.Route(client.State.Stage);
    /// </code>
    /// </example>
    public sealed class SceneRouter : IDisposable
    {
        private const string ScenesFolder = "Assets/Scenes/";

        private readonly IClientLog log;
        private readonly IDisposable subscription;

        /// <summary>Passa a seguir as transições do app.</summary>
        /// <example><code>SceneRouter router = new SceneRouter(composed.Client);</code></example>
        public SceneRouter(AnathemaClient client)
        {
            AnathemaClient required = client ?? throw new ArgumentNullException(nameof(client), "client is null: expected the composed AnathemaClient");
            log = required.Log;
            subscription = required.StageChanged.Subscribe(change => Route(change.Current.Stage));
        }

        /// <summary>Carrega a cena do estágio, se ele tem cena e ela ainda não está carregada.</summary>
        /// <example><code>router.Route(ClientStage.SignedOut);</code></example>
        public void Route(ClientStage stage)
        {
            string? scene = SceneRoute.For(stage);
            if (scene == null || SceneManager.GetSceneByName(scene).isLoaded)
                return;

            log.Info("scene_route", new LogField("stage", stage.ToString()), new LogField("scene", scene));
            if (!Application.CanStreamedLevelBeLoaded(scene) && LoadOutsideSceneList(scene))
                return;

            SceneManager.LoadScene(scene);
        }

        private bool LoadOutsideSceneList(string scene)
        {
#if UNITY_EDITOR
            // O jogador virtual do Multiplayer Play Mode (clone em Library/VP) não enxergou a lista de cenas do projeto,
            // embora leia o mesmo ProjectSettings (quickstart §4 da 005, 2026-09-15). Só no editor, carrega pelo caminho
            // do arquivo; no build a lista de cenas vale sempre.
            string path = ScenesFolder + scene + ".unity";
            log.Warning("scene_route_outside_scene_list", new LogField("scene", scene), new LogField("path", path));
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
#else
            return false;
#endif
        }

        /// <summary>Para de seguir o app.</summary>
        /// <example><code>router.Dispose();</code></example>
        public void Dispose() => subscription.Dispose();
    }
}
