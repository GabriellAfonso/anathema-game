#nullable enable
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>Visões e eventos das fixtures de contrato da partida (research R14 da 004), com trocas de texto por teste.</summary>
    internal static class ProofFixtures
    {
        internal const string AllEvents = "contract-match-update-all-events.json";

        private static readonly IProtocolCodec Codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), new FakeClientLog());

        internal static PlayerView View(string name, params (string Find, string Replace)[] edits)
        {
            ServerFrame frame = Frame(name, edits);
            return frame is MatchStartFrame start ? start.View : ((MatchUpdateFrame)frame).View;
        }

        internal static T Event<T>(params (string Find, string Replace)[] edits) where T : MatchEvent
        {
            return ((MatchUpdateFrame)Frame(AllEvents, edits)).Events.OfType<T>().First();
        }

        private static ServerFrame Frame(string name, (string Find, string Replace)[] edits)
        {
            string text = File.ReadAllText(Path.Combine(FixturesDirectory(), name));
            foreach ((string find, string replace) in edits)
                text = text.Replace(find, replace);

            return Codec.Decode(text).Value;
        }

        private static string FixturesDirectory([CallerFilePath] string sourcePath = "")
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "..", "Net.Match", "Fixtures");
        }
    }
}
