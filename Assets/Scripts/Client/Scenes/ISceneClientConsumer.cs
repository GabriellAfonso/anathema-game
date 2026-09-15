#nullable enable
using Anathema.Net.Facade;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// Um componente de cena que recebe a fachada. O hospedeiro liga cada consumidor uma vez, nas cenas abertas antes
    /// dele e nas carregadas depois: nenhum script procura a fachada nem guarda em campo estático
    /// (specs/005-presentation-facade/contracts/presentation-surface.md, "Regras de consumo").
    /// </summary>
    /// <example>
    /// <code>
    /// public void BindClient(AnathemaClient client)
    /// {
    ///     subscriptions.Add(client.StageChanged.Subscribe(change => Show(change.Current)));
    ///     Show(client.State);
    /// }
    /// </code>
    /// </example>
    public interface ISceneClientConsumer
    {
        /// <summary>Recebe a fachada; chamado uma vez por componente, na thread principal.</summary>
        /// <example><code>public void BindClient(AnathemaClient client) => Show(client.State);</code></example>
        void BindClient(AnathemaClient client);
    }
}
