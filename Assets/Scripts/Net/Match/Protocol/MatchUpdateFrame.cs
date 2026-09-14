#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>match_update</c>: o estado inteiro depois de toda mudança aceita, com os eventos em ordem
    /// (<c>backend/specs/009-match-protocol/contracts/server_frames.md</c>). Versão menor ou igual à última
    /// aplicada é descartada inteira.
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is MatchUpdateFrame update) Apply(update.Version, update.View, update.Events);
    /// </code>
    /// </example>
    public sealed class MatchUpdateFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(MatchUpdateFrame.TypeName, MatchUpdateFrame.Read);</code></example>
        public const string TypeName = "match_update";

        /// <summary>Frame com versão, visão, eventos e relógio.</summary>
        /// <example><code>MatchUpdateFrame update = new MatchUpdateFrame(5, view, events, clock);</code></example>
        public MatchUpdateFrame(long version, PlayerView view, IReadOnlyList<MatchEvent> events, ClockView clock)
            : base(TypeName)
        {
            Version = version;
            View = view ?? throw new ArgumentNullException(nameof(view), $"match_update view of version {version} is null: expected the PlayerView read from the payload");
            Events = events ?? throw new ArgumentNullException(nameof(events), $"match_update events of version {version} are null: expected the event list, possibly empty");
            Clock = clock ?? throw new ArgumentNullException(nameof(clock), $"match_update clock of version {version} is null: expected ClockView.Empty when absent");
        }

        /// <summary>Versão de escrita da partida; não é contínua.</summary>
        /// <example><code>long version = update.Version;</code></example>
        public long Version { get; }

        /// <summary>A visão inteira.</summary>
        /// <example><code>PlayerView view = update.View;</code></example>
        public PlayerView View { get; }

        /// <summary>Eventos na ordem do servidor: jogada, depois consequências.</summary>
        /// <example><code>int count = update.Events.Count;</code></example>
        public IReadOnlyList<MatchEvent> Events { get; }

        /// <summary>O relógio medido ao montar o frame.</summary>
        /// <example><code>ClockView clock = update.Clock;</code></example>
        public ClockView Clock { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>MatchUpdateFrame update = MatchUpdateFrame.Read(payload);</code></example>
        public static MatchUpdateFrame Read(IPayloadReader payload)
        {
            PlayerView view = PlayerView.Read(payload.ReadObject("view"));
            return new MatchUpdateFrame(payload.ReadInteger("version"), view, MatchEvents.ReadList(payload, "events"), ClockView.ReadOptional(payload));
        }
    }
}
