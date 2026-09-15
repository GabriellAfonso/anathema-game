#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// Entrega a fachada aos componentes de cena que a pedem: percorre as raízes de cada cena carregada e chama
    /// <see cref="ISceneClientConsumer.BindClient"/> uma vez por componente. É o ponto de acesso da borda, por injeção,
    /// sem campo estático nem busca por tipo (specs/005-presentation-facade/research.md, R9).
    /// </summary>
    /// <example>
    /// <code>
    /// SceneClientBinder binder = new SceneClientBinder(client);
    /// binder.BindLoadedScenes();
    /// binder.BindScene(gameObject.scene);
    /// </code>
    /// </example>
    public sealed class SceneClientBinder : IDisposable
    {
        private readonly AnathemaClient client;
        private readonly HashSet<ISceneClientConsumer> bound = new HashSet<ISceneClientConsumer>();

        /// <summary>Passa a ligar as cenas carregadas daqui em diante.</summary>
        /// <example><code>SceneClientBinder binder = new SceneClientBinder(composed.Client);</code></example>
        public SceneClientBinder(AnathemaClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client), "client is null: expected the composed AnathemaClient");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>Liga os consumidores das cenas já abertas, como a de login aberta pelo editor junto da de bootstrap.</summary>
        /// <example><code>binder.BindLoadedScenes();</code></example>
        public void BindLoadedScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                BindScene(SceneManager.GetSceneAt(index));
        }

        /// <summary>Liga os consumidores de uma cena, inclusive a de objetos que sobrevivem à troca de cena.</summary>
        /// <example><code>binder.BindScene(gameObject.scene);</code></example>
        public void BindScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
                BindConsumers(root.GetComponentsInChildren<ISceneClientConsumer>(true));
        }

        /// <summary>Para de ligar cenas novas.</summary>
        /// <example><code>binder.Dispose();</code></example>
        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            bound.Clear();
        }

        private void BindConsumers(ISceneClientConsumer[] consumers)
        {
            foreach (ISceneClientConsumer consumer in consumers)
            {
                if (bound.Add(consumer))
                    Bind(consumer);
            }
        }

        private void Bind(ISceneClientConsumer consumer)
        {
            try
            {
                consumer.BindClient(client);
            }
            catch (Exception failure)
            {
                // Uma tela que falha ao ligar não impede as outras de receber a fachada.
                client.Log.Error("scene_bind_failed", new LogField("consumer", consumer.GetType().Name), new LogField("exception", failure.GetType().Name), new LogField("message", failure.Message));
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindScene(scene);

        // Componentes destruídos com a cena saem do conjunto: o Unity compara objeto destruído com null.
        private void OnSceneUnloaded(Scene scene) => bound.RemoveWhere(consumer => consumer as UnityEngine.Object == null);
    }
}
