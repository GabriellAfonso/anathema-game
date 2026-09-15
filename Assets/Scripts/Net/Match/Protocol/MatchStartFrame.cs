#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>match_start</c>: o estado inteiro, ao conectar e a cada reconexão
    /// (<c>backend/specs/009-match-protocol/contracts/server_frames.md</c>). Não traz eventos. A mesma versão
    /// de antes numa reconexão não troca o estado, mas reancora o relógio (specs/004-match-session/research.md, R4).
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is MatchStartFrame start) Resync(start.Version, start.View, start.Clock);
    /// </code>
    /// </example>
    internal sealed class MatchStartFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(MatchStartFrame.TypeName, MatchStartFrame.Read);</code></example>
        public const string TypeName = "match_start";

        /// <summary>Frame com versão, visão e relógio.</summary>
        /// <example><code>MatchStartFrame start = new MatchStartFrame(4, view, ClockView.Empty);</code></example>
        public MatchStartFrame(long version, PlayerView view, ClockView clock)
            : base(TypeName)
        {
            Version = version;
            View = view ?? throw new ArgumentNullException(nameof(view), $"match_start view of version {version} is null: expected the PlayerView read from the payload");
            Clock = clock ?? throw new ArgumentNullException(nameof(clock), $"match_start clock of version {version} is null: expected ClockView.Empty when absent");
        }

        /// <summary>Versão de escrita da partida; não é contínua.</summary>
        /// <example><code>long version = start.Version;</code></example>
        public long Version { get; }

        /// <summary>A visão inteira.</summary>
        /// <example><code>PlayerView view = start.View;</code></example>
        public PlayerView View { get; }

        /// <summary>O relógio medido ao montar o frame.</summary>
        /// <example><code>ClockView clock = start.Clock;</code></example>
        public ClockView Clock { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>MatchStartFrame start = MatchStartFrame.Read(payload);</code></example>
        internal static MatchStartFrame Read(IPayloadReader payload)
        {
            return new MatchStartFrame(payload.ReadInteger("version"), PlayerView.Read(payload.ReadObject("view")), ClockView.ReadOptional(payload));
        }
    }
}
