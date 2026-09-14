#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;

namespace Anathema.Net.Account.Tests
{
    /// <summary>
    /// Codec real para os testes de conta. Não é I/O: ler e escrever JSON de verdade aqui evita
    /// um segundo leitor só para teste, que se afastaria do que o jogo usa.
    /// </summary>
    internal static class AccountTestCodec
    {
        internal static IProtocolCodec Codec(FakeClientLog? log = null)
        {
            return new NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log ?? new FakeClientLog());
        }

        internal static IPayloadReader Reader(string json)
        {
            return Codec().DecodeObject(json).Value;
        }
    }
}
