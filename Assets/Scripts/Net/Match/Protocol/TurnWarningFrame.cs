#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>turn_warning</c>: só ao dono da vez, uma vez por vez, podendo repetir
    /// (<c>backend/specs/010-match-timers/contracts/server_frames.md</c>). Não muda a partida e não tem versão;
    /// o de outra vez é ignorado pelo <see cref="TurnClock"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is TurnWarningFrame warning) clock.NoteWarning(warning, arrival);
    /// </code>
    /// </example>
    internal sealed class TurnWarningFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c> do frame.</summary>
        /// <example><code>union.Register(TurnWarningFrame.TypeName, TurnWarningFrame.Read);</code></example>
        public const string TypeName = "turn_warning";

        /// <summary>Aviso com a vez, o dono e o restante medido.</summary>
        /// <example><code>TurnWarningFrame warning = new TurnWarningFrame(12, new UserId(7), 15000);</code></example>
        public TurnWarningFrame(long turnNumber, UserId holder, long remainingMs)
            : base(TypeName)
        {
            TurnNumber = turnNumber;
            Holder = holder;
            RemainingMs = remainingMs;
        }

        /// <summary>A vez avisada.</summary>
        /// <example><code>long turn = warning.TurnNumber;</code></example>
        public long TurnNumber { get; }

        /// <summary>O dono da vez.</summary>
        /// <example><code>UserId holder = warning.Holder;</code></example>
        public UserId Holder { get; }

        /// <summary>Milissegundos restantes na medição.</summary>
        /// <example><code>long left = warning.RemainingMs;</code></example>
        public long RemainingMs { get; }

        /// <summary>Lê o payload do frame.</summary>
        /// <example><code>TurnWarningFrame warning = TurnWarningFrame.Read(payload);</code></example>
        internal static TurnWarningFrame Read(IPayloadReader payload)
        {
            return new TurnWarningFrame(payload.ReadInteger("turn_number"), payload.ReadUserId("holder_user_id"), payload.ReadInteger("remaining_ms"));
        }
    }
}
