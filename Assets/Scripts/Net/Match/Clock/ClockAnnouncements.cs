#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>
    /// Os avisos que uma reancoragem do relógio marcou, para sair depois dos avisos do espelho: quem assina
    /// "estado substituído" já lê o relógio novo, e "vez nova" chega depois do estado que a explica
    /// (specs/004-match-session/research.md, R4).
    /// </summary>
    internal sealed class ClockAnnouncements
    {
        private readonly TurnClock clock;
        private TurnView? started;
        private long? runningOut;

        internal ClockAnnouncements(TurnClock clock)
        {
            this.clock = clock;
        }

        internal void MarkStarted(TurnView turn) => started = turn;

        internal void MarkRunningOut(long turnNumber) => runningOut = turnNumber;

        internal void Raise()
        {
            if (started != null)
                clock.RaiseStarted(started);

            if (runningOut.HasValue)
                clock.RaiseRunningOut(runningOut.Value);
        }
    }
}
