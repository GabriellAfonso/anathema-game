#nullable enable
using System.Runtime.CompilerServices;

// O codec de Newtonsoft é interno: só a composição real o monta, e nada fora da camada conhece a biblioteca de JSON
// (specs/005-presentation-facade/research.md, R2).
[assembly: InternalsVisibleTo("Anathema.Net.Unity")]

// Os testes das assemblies da camada decodificam fixtures pelo codec real. A prova final só usa o codec nos testes de
// estratégia, para montar visões (plan.md, Complexity Tracking).
[assembly: InternalsVisibleTo("Anathema.Net.Json.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Account.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
[assembly: InternalsVisibleTo("Anathema.Client.Proof.Tests")]
