#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>O lado do oponente: da mão sai só o tamanho, para desenhar cartas viradas.</summary>
    /// <example>
    /// <code>
    /// DrawFaceDownCards(view.Opponent.HandSize);
    /// </code>
    /// </example>
    public sealed class OpponentSideView : SideView
    {
        private OpponentSideView(IPayloadReader side)
            : base(side)
        {
            HandSize = side.ReadInteger("hand_size");
        }

        /// <summary>Quantas cartas o oponente tem na mão.</summary>
        /// <example><code>long cards = view.Opponent.HandSize;</code></example>
        public long HandSize { get; }

        /// <summary>Lê o objeto <c>opponent</c>.</summary>
        /// <example><code>OpponentSideView opponent = OpponentSideView.Read(view.ReadObject("opponent"));</code></example>
        internal static OpponentSideView Read(IPayloadReader side) => new OpponentSideView(side);
    }
}
