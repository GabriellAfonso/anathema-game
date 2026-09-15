#nullable enable
using System.Runtime.CompilerServices;

// Só o que o contrato da superfície lista é público no núcleo; transporte, socket, codec, uniões, leitores, frames,
// fila da thread principal, ciclo de vida, alcançabilidade, guarda e relógio são internos, e as assemblies da camada
// os usam entre si como amigas (specs/005-presentation-facade/research.md, R2). Apresentação, cenas e prova nunca
// entram nesta lista: usar tipo de máquina fora da camada é erro de compilação.
[assembly: InternalsVisibleTo("Anathema.Net.Json")]
[assembly: InternalsVisibleTo("Anathema.Net.Account")]
[assembly: InternalsVisibleTo("Anathema.Net.Connection")]
[assembly: InternalsVisibleTo("Anathema.Net.Match")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity")]

// A configuração e a checagem de build usam o host de servidor e a regra de TLS do núcleo.
[assembly: InternalsVisibleTo("Anathema.Config")]
[assembly: InternalsVisibleTo("Anathema.Net.Editor")]

// Os fakes implementam as portas internas; a assembly de fakes só existe no editor com UNITY_INCLUDE_TESTS.
[assembly: InternalsVisibleTo("Anathema.Net.Fakes")]

// Os testes de cada assembly da camada montam frames, portas e leitores internos. A prova final só monta catálogo e
// visões nos testes de estratégia (plan.md, Complexity Tracking).
[assembly: InternalsVisibleTo("Anathema.Net.Core.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Json.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Account.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
[assembly: InternalsVisibleTo("Anathema.Config.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Editor.Tests")]
[assembly: InternalsVisibleTo("Anathema.Client.Proof.Tests")]
