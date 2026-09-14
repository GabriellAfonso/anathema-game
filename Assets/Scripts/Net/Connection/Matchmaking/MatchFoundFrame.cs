#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// <c>match_found</c> do socket de fila, tipado. Campo ausente ou de tipo errado vira decodificação
    /// inválida com o caminho, pela infraestrutura do codec.
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is MatchFoundFrame found) OnPaired(found.Pairing);
    /// </code>
    /// </example>
    public sealed class MatchFoundFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(MatchFoundFrame.TypeName, MatchFoundFrame.Read);</code></example>
        public const string TypeName = "match_found";

        /// <summary>Frame com o pareamento.</summary>
        /// <example><code>MatchFoundFrame frame = new MatchFoundFrame(pairing);</code></example>
        public MatchFoundFrame(MatchPairing pairing)
            : base(TypeName)
        {
            Pairing = pairing ?? throw new ArgumentNullException(nameof(pairing), "match_found pairing is null: expected the pairing read from the payload");
        }

        /// <summary>A partida e os dois jogadores.</summary>
        /// <example><code>MatchId match = frame.Pairing.Match;</code></example>
        public MatchPairing Pairing { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>MatchFoundFrame frame = MatchFoundFrame.Read(payload);</code></example>
        public static MatchFoundFrame Read(IPayloadReader payload)
        {
            PairedPlayer self = PairedPlayer.Read(payload.ReadObject("self"));
            PairedPlayer opponent = PairedPlayer.Read(payload.ReadObject("opponent"));
            return new MatchFoundFrame(new MatchPairing(payload.ReadMatchId("match_id"), self, opponent));
        }
    }
}
