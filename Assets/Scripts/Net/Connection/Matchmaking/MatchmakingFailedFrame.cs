#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// O pareamento aconteceu no servidor mas a partida não nasceu (perfil ausente, deck recusado na
    /// última guarda). Não é o caminho das recusas de deck, que chegam antes da fila (FR-032).
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is MatchmakingFailedFrame failed) log.Warning("matchmaking_failed", new LogField("error", failed.Error));
    /// </code>
    /// </example>
    public sealed class MatchmakingFailedFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(MatchmakingFailedFrame.TypeName, MatchmakingFailedFrame.Read);</code></example>
        public const string TypeName = "matchmaking_failed";

        /// <summary>Frame com o texto do servidor.</summary>
        /// <example><code>MatchmakingFailedFrame frame = new MatchmakingFailedFrame("no profile for one of the paired users");</code></example>
        public MatchmakingFailedFrame(string error)
            : base(TypeName)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error), "matchmaking_failed error is null: expected the server error text");
        }

        /// <summary>Texto do servidor, só para leitura humana.</summary>
        /// <example><code>string reason = frame.Error;</code></example>
        public string Error { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>MatchmakingFailedFrame frame = MatchmakingFailedFrame.Read(payload);</code></example>
        public static MatchmakingFailedFrame Read(IPayloadReader payload)
        {
            return new MatchmakingFailedFrame(payload.ReadText("error"));
        }
    }
}
