#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam as peças internas da partida (entrada de frames no espelho e no relógio, leitor de
// recusa, narrador, montagem dos comandos): elas não fazem parte da superfície que a apresentação e a
// fachada da feature 5 consomem, e expô-las como públicas só para testar vazaria detalhe de implementação.
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]

// A fachada cria a sessão de partida sobre a conexão composta por ela; os testes dela verificam os avisos da
// sessão (specs/005-presentation-facade/research.md, R2).
[assembly: InternalsVisibleTo("Anathema.Net.Facade")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]

// A borda monta o codec com os frames de partida, que desde a 005 são internos; os testes da borda também.
[assembly: InternalsVisibleTo("Anathema.Net.Unity")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]

// A estratégia da prova final é testada com visões decodificadas pelos frames internos; a assembly da prova não é
// amiga (plan.md, Complexity Tracking).
[assembly: InternalsVisibleTo("Anathema.Client.Proof.Tests")]
