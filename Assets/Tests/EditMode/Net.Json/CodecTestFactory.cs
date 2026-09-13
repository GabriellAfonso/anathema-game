#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;

namespace Anathema.Net.Json.Tests
{
    /// <summary>Codec e reader prontos para os testes deste assembly.</summary>
    internal static class CodecTestFactory
    {
        internal static NewtonsoftProtocolCodec Codec(FakeClientLog? log = null)
        {
            return new NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log ?? new FakeClientLog());
        }

        internal static IPayloadReader Reader(string json)
        {
            return Codec().DecodeObject(json).Value;
        }
    }
}
