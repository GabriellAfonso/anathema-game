#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Match
{
    /// <summary>Lê o texto de <c>phase</c>; texto fora do conjunto vira <see cref="MatchPhase.Unknown"/>.</summary>
    internal static class MatchPhaseText
    {
        private static readonly Dictionary<string, MatchPhase> Phases = new Dictionary<string, MatchPhase>(StringComparer.Ordinal)
        {
            ["mulligan"] = MatchPhase.Mulligan,
            ["upkeep"] = MatchPhase.Upkeep,
            ["action"] = MatchPhase.Action,
            ["declaration"] = MatchPhase.Declaration,
            ["combat"] = MatchPhase.Combat,
            ["round_end"] = MatchPhase.RoundEnd,
            ["finished"] = MatchPhase.Finished,
        };

        internal static MatchPhase Parse(string text)
        {
            return Phases.TryGetValue(text, out MatchPhase phase) ? phase : MatchPhase.Unknown;
        }
    }
}
