#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// A vez como o servidor mediu ao montar o frame (<c>backend/specs/010-match-timers/contracts/server_frames.md</c>).
    /// <see cref="RemainingMs"/> é da medição, não de agora: o restante atual sai do <see cref="TurnClock"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// TurnView? turn = clock.Turn;
    /// if (turn != null) holderLabel.text = turn.Holder.ToString();
    /// </code>
    /// </example>
    public sealed class TurnView
    {
        /// <summary>Vez com número, dono, restante medido e aviso.</summary>
        /// <example><code>TurnView turn = new TurnView(12, new UserId(7), 25000, false);</code></example>
        public TurnView(long turnNumber, UserId holder, long remainingMs, bool warning)
        {
            TurnNumber = turnNumber;
            Holder = holder;
            RemainingMs = remainingMs;
            Warning = warning;
        }

        /// <summary>Identidade da vez; muda a cada vez nova, mesmo com o mesmo dono.</summary>
        /// <example><code>long number = turn.TurnNumber;</code></example>
        public long TurnNumber { get; }

        /// <summary>Quem deve a jogada.</summary>
        /// <example><code>UserId holder = turn.Holder;</code></example>
        public UserId Holder { get; }

        /// <summary>Milissegundos restantes na medição.</summary>
        /// <example><code>long measured = turn.RemainingMs;</code></example>
        public long RemainingMs { get; }

        /// <summary>O servidor marcou a vez como perto do fim.</summary>
        /// <example><code>bool late = turn.Warning;</code></example>
        public bool Warning { get; }

        /// <summary>Lê o objeto <c>turn</c>.</summary>
        /// <example><code>TurnView turn = TurnView.Read(clock.ReadObject("turn"));</code></example>
        public static TurnView Read(IPayloadReader turn)
        {
            return new TurnView(turn.ReadInteger("turn_number"), turn.ReadUserId("holder_user_id"), turn.ReadInteger("remaining_ms"), turn.ReadBoolean("warning"));
        }
    }
}
