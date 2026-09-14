#nullable enable
using System;

namespace Anathema.Net.Match.Tests
{
    /// <summary>Visões prontas para os testes do espelho, a partir das fixtures de contrato.</summary>
    internal static class MirrorViews
    {
        internal static PlayerView Mulligan() => View("contract-match-start-mulligan.json");

        internal static PlayerView Action() => View("contract-match-update-action.json");

        internal static PlayerView Declaration() => View("contract-match-update-declaration.json");

        internal static PlayerView Finished() => View("contract-match-update-finished.json");

        internal static PlayerView Edited(string name, string find, string replace)
        {
            string text = MatchFixtures.Text(name);
            if (!text.Contains(find))
                throw new ArgumentException($"fixture '{name}' has no '{find}': expected text present in the fixture", nameof(find));

            return StateView(text.Replace(find, replace));
        }

        private static PlayerView View(string name) => StateView(MatchFixtures.Text(name));

        private static PlayerView StateView(string frameJson)
        {
            return MatchJson.Decode(frameJson).Value switch
            {
                MatchStartFrame start => start.View,
                MatchUpdateFrame update => update.View,
                var other => throw new ArgumentException($"frame is {other.MessageType}: expected match_start or match_update", nameof(frameJson)),
            };
        }
    }
}
