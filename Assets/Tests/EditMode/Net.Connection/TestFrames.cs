#nullable enable

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Textos de frame do servidor usados nos roteiros.</summary>
    internal static class TestFrames
    {
        internal const string Pong = "{\"type\": \"pong\", \"payload\": {}}";
        internal const string AuthDenied = "{\"type\": \"auth_denied\", \"payload\": {\"error\": \"authentication required: invalid token\"}}";
        internal const string MatchDenied = "{\"type\": \"match_denied\", \"payload\": {\"error\": \"no live match\"}}";

        internal const string MatchFound = "{\"type\": \"match_found\", \"payload\": {\"self\": {\"user_id\": 7, \"nickname\": \"one\", \"icon\": \"default\", \"level\": 1}, \"opponent\": {\"user_id\": 9, \"nickname\": \"two\", \"icon\": \"knight\", \"level\": 3}, \"match_id\": \"match-7\"}}";
        internal const string MatchmakingFailed = "{\"type\": \"matchmaking_failed\", \"payload\": {\"error\": \"no profile for one of the paired users\"}}";
        internal const string DeckNotFound = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"deck_id 4 is not a deck of user 9\", \"code\": \"deck_not_found\", \"deck_id\": 4}}";

        internal const string InvalidDeck = "{\"type\": \"message_refused\", \"payload\": {\"error\": \"deck has 39 cards, expected exactly 40; card_id 12 appears 4 times, limit is 3\", \"code\": \"invalid_deck\", \"deck_id\": 4, \"deck_problems\": ["
            + "{\"kind\": \"wrong_deck_size\", \"found\": 39, \"required\": 40, \"message\": \"deck has 39 cards, expected exactly 40\"}, "
            + "{\"kind\": \"too_many_copies\", \"card_id\": 12, \"count\": 4, \"limit\": 3, \"message\": \"card_id 12 appears 4 times, limit is 3\"}]}}";

        internal static string Refused(string code) => "{\"type\": \"message_refused\", \"payload\": {\"error\": \"refused\", \"code\": \"" + code + "\"}}";

        /// <summary>O pong que o servidor devolve para um ping mandado: o mesmo payload, ecoado.</summary>
        internal static string PongFor(string pingText)
        {
            const string PayloadKey = "\"payload\":";
            int start = pingText.IndexOf(PayloadKey, System.StringComparison.Ordinal) + PayloadKey.Length;
            return "{\"type\":\"pong\",\"payload\":" + pingText.Substring(start, pingText.Length - start - 1) + "}";
        }

        internal static string Unknown(string type) => "{\"type\": \"" + type + "\", \"payload\": {}}";
    }
}
