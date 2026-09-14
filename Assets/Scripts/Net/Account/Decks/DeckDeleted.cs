#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Deck apagado (204). Some da listagem; uma leitura seguinte dá deck não encontrado.</summary>
    /// <example><code>if (deleted.IsSuccess) RemoveFromList(deck);</code></example>
    public sealed class DeckDeleted
    {
        private DeckDeleted()
        {
        }

        /// <summary>O único valor, porque não há o que carregar.</summary>
        /// <example><code>return AccountCallOutcome&lt;DeckDeleted, DeckRefusal&gt;.Success(DeckDeleted.Instance);</code></example>
        public static DeckDeleted Instance { get; } = new DeckDeleted();
    }
}
