#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O gate da partida recusou o socket (sem matchId, jogador de fora, partida inexistente). Chega
    /// antes do fechamento 4400, 4403 ou 4404, e reconectar não adianta. É lido pela conexão, não pela
    /// partida, por isso mora com os frames genéricos (specs/003-authenticated-socket-queue/research.md, R1).
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is MatchDeniedFrame denied) log.Warning("match_denied", new LogField("error", denied.Error));
    /// </code>
    /// </example>
    internal sealed class MatchDeniedFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(MatchDeniedFrame.TypeName, MatchDeniedFrame.Read);</code></example>
        public const string TypeName = "match_denied";

        /// <summary>Frame com o texto do servidor.</summary>
        /// <example><code>MatchDeniedFrame frame = new MatchDeniedFrame("no live match 'x'");</code></example>
        public MatchDeniedFrame(string error)
            : base(TypeName)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error), "match_denied error is null: expected the server error text");
        }

        /// <summary>Texto do servidor, só para leitura humana.</summary>
        /// <example><code>string reason = frame.Error;</code></example>
        public string Error { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>MatchDeniedFrame frame = MatchDeniedFrame.Read(payload);</code></example>
        internal static MatchDeniedFrame Read(IPayloadReader payload)
        {
            return new MatchDeniedFrame(payload.ReadText("error"));
        }
    }
}
