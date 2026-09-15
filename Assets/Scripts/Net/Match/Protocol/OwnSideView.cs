#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>O próprio lado: a mão aparece inteira, com a cópia de cada carta.</summary>
    /// <example>
    /// <code>
    /// foreach (MatchCard card in view.You.Hand) DrawHandCard(card);
    /// </code>
    /// </example>
    public sealed class OwnSideView : SideView
    {
        private OwnSideView(IPayloadReader side)
            : base(side)
        {
            Hand = MatchCard.ReadList(side, "hand");
        }

        /// <summary>A mão, na ordem do servidor.</summary>
        /// <example><code>CardInstanceId first = view.You.Hand[0].Instance;</code></example>
        public IReadOnlyList<MatchCard> Hand { get; }

        /// <summary>Lê o objeto <c>you</c>.</summary>
        /// <example><code>OwnSideView you = OwnSideView.Read(view.ReadObject("you"));</code></example>
        internal static OwnSideView Read(IPayloadReader side) => new OwnSideView(side);
    }
}
