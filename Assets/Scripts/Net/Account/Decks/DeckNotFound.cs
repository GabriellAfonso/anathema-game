#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// 404: o deck não existe, ou não é do autenticado. O servidor responde igual nos dois casos, de
    /// propósito, e o cliente não tem como nem por que distinguir.
    /// </summary>
    /// <example><code>if (reason is DeckNotFound) RefreshList();</code></example>
    public sealed class DeckNotFound : DeckRefusalReason
    {
        private DeckNotFound()
        {
        }

        /// <summary>O único valor.</summary>
        /// <example><code>return DeckRefusal.Of(DeckNotFound.Instance);</code></example>
        public static DeckNotFound Instance { get; } = new DeckNotFound();
    }
}
