#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;

namespace Anathema.Net.Match.Tests
{
    /// <summary>Codec real da partida para os testes de forma: frame decodificado e objeto solto.</summary>
    internal static class MatchJson
    {
        internal static IProtocolCodec Codec() => new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), new FakeClientLog());

        internal static DecodeOutcome<ServerFrame> Decode(string frameJson) => Codec().Decode(frameJson);

        internal static T Frame<T>(string frameJson) where T : ServerFrame => (T)Decode(frameJson).Value;

        internal static T Fixture<T>(string name) where T : ServerFrame => Frame<T>(MatchFixtures.Text(name));

        internal static IPayloadReader Reader(string json) => Codec().DecodeObject(json).Value;

        internal static string Encode(IOutgoingMessage message) => Codec().Encode(message);
    }
}
