#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Codec real com a união da camada, para os testes deste assembly.</summary>
    internal static class ConnectionTestCodec
    {
        internal static IProtocolCodec Codec(FakeClientLog? log = null)
        {
            return new NewtonsoftProtocolCodec(ConnectionFrames.CreateUnion(), log ?? new FakeClientLog());
        }
    }
}
