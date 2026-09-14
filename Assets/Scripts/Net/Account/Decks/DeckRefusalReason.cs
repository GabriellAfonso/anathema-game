#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// Um motivo de recusa de uma operação de deck. Hierarquia fechada (constituição, princípio IV):
    /// <see cref="InvalidDeckName"/>, <see cref="DeckListRejected"/>, <see cref="MissingDeckField"/>,
    /// <see cref="DeckLimitReached"/>, <see cref="DeckNotFound"/> e <see cref="UnrecognizedDeckRefusal"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (DeckRefusalReason reason in refusal.Reasons)
    ///     if (reason is DeckListRejected rejected) ShowProblems(rejected.Problems);
    /// </code>
    /// </example>
    public abstract class DeckRefusalReason
    {
        private protected DeckRefusalReason()
        {
        }
    }
}
