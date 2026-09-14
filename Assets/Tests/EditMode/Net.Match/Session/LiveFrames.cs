#nullable enable
using System.Text.RegularExpressions;

namespace Anathema.Net.Match.Tests
{
    /// <summary>Frames das fixtures de contrato com versão, tipo ou restante trocados, para roteirizar a sessão.</summary>
    internal static class LiveFrames
    {
        internal static string StartMulligan(long version) => WithVersion(MatchFixtures.Text("contract-match-start-mulligan.json"), version);

        internal static string UpdateAction(long version, long remainingMs = 25000) => WithRemaining(WithVersion(MatchFixtures.Text("contract-match-update-action.json"), version), remainingMs);

        internal static string StartAction(long version, long remainingMs = 25000) => AsStart(UpdateAction(version, remainingMs));

        internal static string UpdateFinished(long version) => WithVersion(MatchFixtures.Text("contract-match-update-finished.json"), version);

        internal static string StartFinished(long version) => AsStart(UpdateFinished(version));

        internal static string Refused(string code) => "{\"type\": \"message_refused\", \"payload\": {\"code\": \"" + code + "\", \"error\": \"refused by the engine\"}}";

        internal static string WarningFor(long turnNumber) => "{\"type\": \"turn_warning\", \"payload\": {\"turn_number\": " + turnNumber + ", \"holder_user_id\": 9, \"remaining_ms\": 15000}}";

        internal const string Pong = "{\"type\": \"pong\", \"payload\": {}}";

        internal const string MatchDenied = "{\"type\": \"match_denied\", \"payload\": {\"error\": \"no live match\"}}";

        internal const string AuthDenied = "{\"type\": \"auth_denied\", \"payload\": {\"error\": \"invalid token\"}}";

        private static string WithVersion(string frame, long version) => Regex.Replace(frame, "\"version\": \\d+", "\"version\": " + version);

        private static string WithRemaining(string frame, long remainingMs) => frame.Replace("\"remaining_ms\": 25000", "\"remaining_ms\": " + remainingMs);

        private static string AsStart(string frame) => frame.Replace("\"type\": \"match_update\"", "\"type\": \"match_start\"");
    }
}
